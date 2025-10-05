using System;
using System.Threading.Tasks;

namespace VitalRouter
{
public class ReceiveContext<T> where T : ICommand
{
    public static async ValueTask InvokeAsync(T command, ICommandInterceptor[] interceptorStack, PublishContext context)
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

    ICommandInterceptor[] interceptors = default!;
    int currentInterceptorIndex = -1;

    readonly PublishContinuation<T> continuation;

    public static ReceiveContext<T> Rent(ICommandInterceptor[] interceptors)
    {
        var value = ContextPool<ReceiveContext<T>>.Rent();
        value.interceptors = interceptors;
        return value;
    }

    public ReceiveContext()
    {
        continuation = InvokeRecursiveAsync;
    }

    public ValueTask InvokeRecursiveAsync(T command, PublishContext context)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, context, continuation);
        }
        return default;
    }

    public void Return()
    {
        interceptors = null!;
        currentInterceptorIndex = -1;
        ContextPool<ReceiveContext<T>>.Return(this);
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
}