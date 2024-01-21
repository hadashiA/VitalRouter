using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public class PublishContext
{
    public CancellationToken CancellationToken { get; set; }
    public string? CallerMemberName { get; set; }
    public int CallerLineNumber { get; set; }
    public string? CallerFilePath { get; set; }
    public IDictionary<string, object?> Extensions { get; } = new ConcurrentDictionary<string, object?>();

    static readonly ConcurrentQueue<PublishContext> Pool = new(new []
    {
        new PublishContext(),
        new PublishContext(),
        new PublishContext(),
        new PublishContext(),
    });

    public static PublishContext Rent(
        CancellationToken cancellation,
        string? callerMemberName,
        string? callerFilePath,
        int callerLineNumber)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new PublishContext();
        }
        value.CancellationToken = cancellation;
        value.CallerMemberName = callerMemberName;
        value.CallerFilePath = callerFilePath;
        value.CallerLineNumber = callerLineNumber;
        return value;
    }

    public virtual void Return()
    {
        Pool.Enqueue(this);
    }
}

public class PublishContext<T> : PublishContext where T : ICommand
{
    static readonly ConcurrentQueue<PublishContext<T>> Pool = new(new []
    {
        new PublishContext<T>(),
        new PublishContext<T>(),
        new PublishContext<T>(),
        new PublishContext<T>(),
    });

    FreeList<ICommandInterceptor> interceptors = default!;
    IAsyncCommandSubscriber core = default!;
    int currentInterceptorIndex = -1;

    readonly PublishContinuation<T> continuation;

    public static PublishContext<T> Rent(
        FreeList<ICommandInterceptor> interceptors,
        IAsyncCommandSubscriber core,
        CancellationToken cancellation,
        string? callerMemberName,
        string? callerFilePath,
        int callerLineNumber)
    {
        if (!Pool.TryDequeue(out var value))
        {
            value = new PublishContext<T>();
        }
        value.interceptors = interceptors;
        value.core = core;
        value.CancellationToken = cancellation;
        value.CallerMemberName = callerMemberName;
        value.CallerFilePath = callerFilePath;
        value.CallerLineNumber = callerLineNumber;
        return value;
    }

    PublishContext()
    {
        continuation = InvokeRecursiveAsync;
    }

    public UniTask PublishAsync(T command)
    {
        return InvokeRecursiveAsync(command, this);
    }

    UniTask InvokeRecursiveAsync(T command, PublishContext context)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, this, continuation);
        }
        return core.ReceiveAsync(command, context);
    }

    public override void Return()
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