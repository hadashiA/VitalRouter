using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public class InvokeContext<T> where T : ICommand
{
    static readonly ConcurrentQueue<InvokeContext<T>> Pool = new();

    IReadOnlyList<ICommandInterceptor> interceptors = default!;
    IAsyncCommandSubscriber? invoker;
    int currentInterceptorIndex = -1;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static InvokeContext<T> Rent(IReadOnlyList<ICommandInterceptor> interceptors)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new InvokeContext<T>();
        }
        value.interceptors = interceptors;
        return value;
    }

    public static InvokeContext<T> Rent(IReadOnlyList<ICommandInterceptor> interceptors, IAsyncCommandSubscriber invoker)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new InvokeContext<T>();
        }
        value.interceptors = interceptors;
        value.invoker = invoker;
        return value;
    }


    InvokeContext()
    {
        nextDelegate = InvokeRecursiveAsync;
    }

    public UniTask InvokeRecursiveAsync(T command, CancellationToken cancellation = default)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, cancellation, nextDelegate);
        }

        if (invoker != null)
        {
            return invoker.ReceiveAsync(command, cancellation);
        }
        return UniTask.CompletedTask;
    }

    public void Return()
    {
        invoker = null;
        invoker = null;
        interceptors = null!;
        currentInterceptorIndex = -1;
        Pool.Enqueue(this);
    }

    public bool MoveNextInterceptor(out ICommandInterceptor nextInterceptor)
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
