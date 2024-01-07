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
        return UniTask.CompletedTask;
    }

    public void Return()
    {
        interceptors = null!;
        currentInterceptorIndex = -1;
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
