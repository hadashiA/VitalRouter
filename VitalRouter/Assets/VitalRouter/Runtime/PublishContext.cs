using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal;

class PublishContext<T> where T : ICommand
{
    static readonly ConcurrentQueue<PublishContext<T>> Pool = new();

    IReadOnlyList<ICommandInterceptor> interceptors = default!;
    CommandBus commandBus = default!;
    int currentInterceptorIndex;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static PublishContext<T> Rent(
        CommandBus commandBus,
        IReadOnlyList<ICommandInterceptor> interceptors)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new PublishContext<T>();
        }

        value.commandBus = commandBus;
        value.currentInterceptorIndex = -1;
        value.interceptors = interceptors;
        return value;
    }

    PublishContext()
    {
        nextDelegate = InvokeRecursiveAsync;
    }

    public UniTask InvokeRecursiveAsync(T command, CancellationToken cancellation = default)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, cancellation, nextDelegate);
        }
        return commandBus.PublishCoreAsync(command, cancellation);
    }

    public void Return()
    {
        Pool.Enqueue(this);
    }

    bool MoveNextInterceptor(out ICommandInterceptor nextInterceptor)
    {
        if (++currentInterceptorIndex <= interceptors.Count - 1)
        {
            nextInterceptor = interceptors[currentInterceptorIndex];
            return true;
        }
        nextInterceptor = default!;
        return false;
    }
}
