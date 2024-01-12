using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public class InvokeContextWithFreeList<T> where T : ICommand
{
    static readonly ConcurrentQueue<InvokeContextWithFreeList<T>> Pool = new();

    FreeList<ICommandInterceptor> interceptors = default!;
    ICommandInterceptor core = default!;
    int currentInterceptorIndex = -1;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static InvokeContextWithFreeList<T> Rent(FreeList<ICommandInterceptor> interceptors, ICommandInterceptor core)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new InvokeContextWithFreeList<T>();
        }
        value.interceptors = interceptors;
        value.core = core;
        return value;
    }

    InvokeContextWithFreeList()
    {
        nextDelegate = InvokeRecursiveAsync;
    }

    public UniTask InvokeRecursiveAsync(T command, CancellationToken cancellation = default)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, cancellation, nextDelegate);
        }
        return core.InvokeAsync(command, cancellation, static (command1, token) => UniTask.CompletedTask);
    }

    public void Return()
    {
        interceptors = null!;
        currentInterceptorIndex = -1;
        Pool.Enqueue(this);
    }

    bool MoveNextInterceptor(out ICommandInterceptor nextInterceptor)
    {
        while (++currentInterceptorIndex <= interceptors.LastIndex)
        {
            if (interceptors[currentInterceptorIndex] is { } x)
            {
                nextInterceptor = x;
                return true;
            }
        }
        nextInterceptor = default!;
        return false;
    }
}