using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public class ReceiveContext<T> where T : ICommand
{
    public static async UniTask InvokeAsync(T command, ICommandInterceptor[] interceptorStack, PublishContext context)
    {
        var invoker = Rent(interceptorStack);
        try
        {
            await invoker.InvokeRecursiveAsync(command, context);
        }
        finally
        {
            invoker.Return();
        }
    }

    static readonly ConcurrentQueue<ReceiveContext<T>> Pool = new(new []
    {
        new ReceiveContext<T>(),
        new ReceiveContext<T>(),
        new ReceiveContext<T>(),
        new ReceiveContext<T>(),
    });

    ICommandInterceptor[] interceptors = default!;
    int currentInterceptorIndex = -1;

    readonly PublishContinuation<T> continuation;

    public static ReceiveContext<T> Rent(ICommandInterceptor[] interceptors)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new ReceiveContext<T>();
        }
        value.interceptors = interceptors;
        return value;
    }

    ReceiveContext()
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