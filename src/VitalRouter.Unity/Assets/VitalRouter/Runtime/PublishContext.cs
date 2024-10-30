using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{

static class ContextPool<T> where T : class, new()
{
    static readonly ConcurrentQueue<T> items = new(new []
    {
        new T(),
        new T(),
        new T(),
        new T(),
    });

    static T? fastItem;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Rent()
    {
        var value = fastItem;
        if (value != null &&
            Interlocked.CompareExchange(ref fastItem, null, value) == value)
        {
            return value;
        }
        if (items.TryDequeue(out value))
        {
            return value;
        }
        return new T();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(T value)
    {
        if (fastItem != null ||
            Interlocked.CompareExchange(ref fastItem, value, null) != null)
        {
            items.Enqueue(value);
        }
    }
}

public partial class PublishContext
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
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static PublishContext Rent(
        CancellationToken cancellation,
        string? callerMemberName,
        string? callerFilePath,
        int callerLineNumber)
    {
        var value = ContextPool<PublishContext>.Rent();
        value.CancellationToken = cancellation;
        value.CallerMemberName = callerMemberName;
        value.CallerFilePath = callerFilePath;
        value.CallerLineNumber = callerLineNumber;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal virtual void Return()
    {
        Extensions.Clear();
        ContextPool<PublishContext>.Return(this);
    }
}

public class PublishContext<T> : PublishContext where T : ICommand
{
    FreeList<ICommandInterceptor> interceptors = default!;
    IAsyncCommandSubscriber core = default!;
    int currentInterceptorIndex = -1;

    readonly PublishContinuation<T> continuation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PublishContext<T> Rent(
        FreeList<ICommandInterceptor> interceptors,
        IAsyncCommandSubscriber core,
        CancellationToken cancellation,
        string? callerMemberName,
        string? callerFilePath,
        int callerLineNumber)
    {
        var value = ContextPool<PublishContext<T>>.Rent();
        value.interceptors = interceptors;
        value.core = core;
        value.CancellationToken = cancellation;
        value.CallerMemberName = callerMemberName;
        value.CallerFilePath = callerFilePath;
        value.CallerLineNumber = callerLineNumber;
        value.Extensions.Clear();
        return value;
    }

    public PublishContext()
    {
        continuation = InvokeRecursiveAsync;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync(T command)
    {
        return InvokeRecursiveAsync(command, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ValueTask InvokeRecursiveAsync(T command, PublishContext context)
    {
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, this, continuation);
        }
        return core.ReceiveAsync(command, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal override void Return()
    {
        interceptors = null!;
        currentInterceptorIndex = -1;
        Extensions.Clear();
        ContextPool<PublishContext<T>>.Return(this);
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
}