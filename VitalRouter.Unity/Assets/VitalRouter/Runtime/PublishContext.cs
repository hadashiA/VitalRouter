using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public class PublishContext
{
    /// <summary>
    /// Cancellation token set by Publisher. Used to cancel this entire Publish.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// The Member name of the caller who published. `[CallerMemberName]` is the source.
    /// </summary>
    public string? CallerMemberName { get; set; }

    /// <summary>
    /// The file full path of the caller who published. `[CallerFilePAth]` is the source.
    /// </summary>
    public string? CallerFilePath { get; set; }

    /// <summary>
    /// The line number of the caller who published. `[CallerLineNumber]` is the source.
    /// </summary>
    public int CallerLineNumber { get; set; }

    /// <summary>
    /// A general-purpose shared data area that is valid only while it is being Publish. (Experimental)
    /// </summary>
    public IDictionary<string, object?> Extensions { get; } = new ConcurrentDictionary<string, object?>();

    static readonly ConcurrentQueue<PublishContext> Pool = new(new []
    {
        new PublishContext(),
        new PublishContext(),
        new PublishContext(),
        new PublishContext(),
    });

    internal static PublishContext Rent(
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

    internal virtual void Return()
    {
        Extensions.Clear();
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

    internal override void Return()
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