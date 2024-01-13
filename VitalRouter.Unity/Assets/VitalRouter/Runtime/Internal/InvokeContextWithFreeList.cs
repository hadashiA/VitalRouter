using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal;

class InvokeContextWithFreeList<T> where T : ICommand
{
    static readonly ConcurrentQueue<InvokeContextWithFreeList<T>> Pool = new();

    FreeList<ICommandInterceptor> interceptors = default!;
    IAsyncCommandSubscriber core = default!;
    int currentInterceptorIndex = -1;

    readonly Func<T, CancellationToken, UniTask> nextDelegate;

    public static InvokeContextWithFreeList<T> Rent(FreeList<ICommandInterceptor> interceptors, IAsyncCommandSubscriber core)
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
        return core.ReceiveAsync(command, cancellation);
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