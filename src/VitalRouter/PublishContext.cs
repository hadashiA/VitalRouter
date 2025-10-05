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
    static readonly ConcurrentQueue<T> Items = new();

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
        if (Items.TryDequeue(out value))
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
            Items.Enqueue(value);
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
    /// A general-purpose shared data area that is valid only while it is being Publish. (Experimental)
    /// </summary>
    public IDictionary<string, object?> Extensions
    {
        get
        {
            return extensions ??= new ConcurrentDictionary<string, object?>();
        }
    }

    protected ConcurrentDictionary<string, object?>? extensions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static PublishContext Rent(CancellationToken cancellation)
    {
        var value = ContextPool<PublishContext>.Rent();
        value.CancellationToken = cancellation;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal virtual void Return()
    {
        extensions?.Clear();
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
    internal static PublishContext<T> Rent(
        FreeList<ICommandInterceptor> interceptors,
        IAsyncCommandSubscriber core,
        CancellationToken cancellation)
    {
        var value = ContextPool<PublishContext<T>>.Rent();
        value.interceptors = interceptors;
        value.core = core;
        value.CancellationToken = cancellation;
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
        extensions?.Clear();
        currentInterceptorIndex = -1;
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