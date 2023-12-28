using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

class PublishContext<T> where T : ICommand
{
    static readonly ConcurrentQueue<PublishContext<T>> Pool = new();

    public T Command { get; set; } = default!;
    public IReadOnlyList<IAsyncCommandInterceptor> Interceptors { get; set; } = default!;
    public ICommandPublisher Publisher { get; set; } = default!;
    int currentInterceptorIndex;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static PublishContext<T> Rent(
        ICommandPublisher publisher,
        IReadOnlyList<IAsyncCommandInterceptor> interceptors)
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
        return Publisher.PublishAsync(command, cancellation);
    }

    public void Return()
    {
        Pool.Enqueue(this);
    }

    bool MoveNextInterceptor(out IAsyncCommandInterceptor nextInterceptor)
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
