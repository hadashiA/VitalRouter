using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public class InvokeContext<T> where T : ICommand
{
    static readonly ConcurrentQueue<InvokeContext<T>> Pool = new(new []
    {
        new InvokeContext<T>(),
        new InvokeContext<T>(),
        new InvokeContext<T>(),
        new InvokeContext<T>(),
    });

    ICommandInterceptor[] interceptors = default!;
    int currentInterceptorIndex = -1;

    readonly PublishContinuation<T> continuation;

    public static InvokeContext<T> Rent(ICommandInterceptor[] interceptors)
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
        continuation = InvokeRecursiveAsync;
    }

    public UniTask InvokeRecursiveAsync(T command, PublishContext context)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, context, continuation);
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
        if (++currentInterceptorIndex <= interceptors.Length - 1)
        {
            nextInterceptor = interceptors[currentInterceptorIndex];
            return true;
        }
        nextInterceptor = default!;
        return false;
    }
}