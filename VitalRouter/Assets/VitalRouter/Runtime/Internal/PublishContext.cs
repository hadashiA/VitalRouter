using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal;

class PublishContext<T> where T : ICommand
{
    static readonly ConcurrentQueue<PublishContext<T>> Pool = new();

    public T Command { get; set; } = default!;
    public IReadOnlyList<ICommandInterceptor> Interceptors { get; set; } = default!;
    public CommandBus Publisher { get; set; } = default!;
    int currentInterceptorIndex;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static PublishContext<T> Rent(
        CommandBus publisher,
        ExpandBuffer<ICommandInterceptor> interceptors)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new PublishContext<T>();
        }
        value.currentInterceptorIndex = -1;
        value.Publisher = publisher;
        value.Interceptors = interceptors;
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
        return Publisher.PublishCoreAsync(command, cancellation);
    }

    public void Return()
    {
        Pool.Enqueue(this);
    }

    bool MoveNextInterceptor(out ICommandInterceptor nextInterceptor)
    {
        if (++currentInterceptorIndex <= Interceptors.Count - 1)
        {
            nextInterceptor = Interceptors[currentInterceptorIndex];
            return true;
        }
        nextInterceptor = default!;
        return false;
    }
}
