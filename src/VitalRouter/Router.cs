using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public interface ICommandPublisher
{
    ValueTask PublishAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand;
    ICommandPublisher WithFilter(ICommandInterceptor interceptor);
}

public interface ICommandSubscribable
{
    Subscription Subscribe(ICommandSubscriber subscriber);
    Subscription Subscribe(IAsyncCommandSubscriber subscriber);
    void Unsubscribe(ICommandSubscriber subscriber);
    void Unsubscribe(IAsyncCommandSubscriber subscriber);
    ICommandSubscribable WithFilter(ICommandInterceptor interceptor);
}

public interface ICommandSubscriber
{
    void Receive<T>(T command, PublishContext context) where T : ICommand;
}

public interface IAsyncCommandSubscriber
{
    ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand;
}

public sealed partial class Router : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly Router Default = new();

    public static Func<IAsyncLock> AsyncLockFactory { get; private set; } = () => new SemaphoreSlimAsyncLock();
    public static Func<CancellationToken, ValueTask> YieldAction { get; private set; } = async _ => await Task.Yield();
    public static Action<string> Logger { get; private set; } = Console.WriteLine;

    public static void RegisterAsyncLock(Func<IAsyncLock> asyncLockFactory) => AsyncLockFactory = asyncLockFactory;
    public static void RegisterYieldAction(Func<CancellationToken, ValueTask> yieldAction) => YieldAction = yieldAction;

    readonly FreeList<ICommandSubscriber> subscribers = new(8);
    readonly FreeList<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    // Cumulative interceptor chain from root to this router. Used when this router
    // is the direct publish entry point (Router.PublishAsync).
    readonly FreeList<ICommandInterceptor> interceptors = new(8);
    // Interceptors added locally at this node only. Used when this router is reached
    // via fan-out from its parent (ancestor filters have already been applied by the
    // parent's pipeline, so we must not re-run them here).
    readonly FreeList<ICommandInterceptor> localInterceptors = new(8);
    readonly FreeList<Router> childRouters = new(4);

    Router? parentRouter;

    bool disposed;
    bool hasInterceptor;
    bool hasLocalInterceptor;

    readonly PublishCore publishCore;

    // This class is intended to be resolved via Dependency Injection (DI).
    // Please note that adding constructors may break the DI functionality.
    [Preserve]
    public Router()
    {
        publishCore = new PublishCore(this);
    }

    /// <summary>
    /// Wires this router as a child of `parent` and snapshots the parent's cumulative
    /// </summary>
    /// <param name="parent"></param>
    /// <remarks>
    /// filter chain into this router's `interceptors`.
    /// </remarks>
    void AttachToParent(Router parent)
    {
        parentRouter = parent;
        foreach (var inherited in parent.interceptors.AsSpan())
        {
            if (inherited != null)
            {
                interceptors.Add(inherited);
                hasInterceptor = true;
            }
        }
    }

    public ValueTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        return PublishInternalAsync(command, cancellation, hasInterceptor, interceptors);
    }

    // Called when this router is reached via fan-out from its parent. The parent's
    // pipeline has already run ancestor filters, so we apply only the locally added
    // ones (avoiding double execution of inherited filters).
    ValueTask FanOutAsync<T>(T command, CancellationToken cancellation)
        where T : ICommand
    {
        return PublishInternalAsync(command, cancellation, hasLocalInterceptor, localInterceptors);
    }

    ValueTask PublishInternalAsync<T>(
        T command,
        CancellationToken cancellation,
        bool hasFilters,
        FreeList<ICommandInterceptor> filters)
        where T : ICommand
    {
        ValueTask task;
        PublishContext context;
        if (hasFilters)
        {
            var c = PublishContext<T>.Rent(filters, publishCore, cancellation);
            context = c;
            task = c.PublishAsync(command);
        }
        else
        {
            context = PublishContext.Rent(cancellation);
            task = publishCore.ReceiveAsync(command, context);
        }

        if (task.IsCompletedSuccessfully)
        {
            context.Return();
            return task;
        }

        return ContinueAsync(task, context);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async ValueTask ContinueAsync(ValueTask x, PublishContext c)
        {
            try
            {
                await x;
            }
            finally
            {
                c.Return();
            }
        }
    }

    public Subscription Subscribe(ICommandSubscriber subscriber)
    {
        subscribers.Add(subscriber);
        return new Subscription(this, subscriber);
    }

    public Subscription Subscribe(IAsyncCommandSubscriber subscriber)
    {
        asyncSubscribers.Add(subscriber);
        return new Subscription(this, subscriber);
    }

    public void Unsubscribe(ICommandSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    public void Unsubscribe(IAsyncCommandSubscriber subscriber)
    {
        asyncSubscribers.Remove(subscriber);
    }

    public void UnsubscribeAll()
    {
        subscribers.Clear();
        asyncSubscribers.Clear();
    }

    public void AddFilter(ICommandInterceptor interceptor)
    {
        hasInterceptor = true;
        interceptors.Add(interceptor);
        hasLocalInterceptor = true;
        localInterceptors.Add(interceptor);
    }

    public void RemoveFilter(ICommandInterceptor interceptor)
    {
        RemoveFilter(x => x == interceptor);
    }

    public void RemoveFilter(Func<ICommandInterceptor, bool> predicate)
    {
        hasInterceptor = RemoveMatchingInterceptors(interceptors, predicate);
        hasLocalInterceptor = RemoveMatchingInterceptors(localInterceptors, predicate);
    }

    static bool RemoveMatchingInterceptors(FreeList<ICommandInterceptor> list, Func<ICommandInterceptor, bool> predicate)
    {
        var span = list.AsSpan();
        var count = 0;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (list[i] is { } x)
            {
                count++;
                if (predicate(x))
                {
                    list.RemoveAt(i);
                    count--;
                }
            }
        }
        return count > 0;
    }

    public void RemoveAllFilters()
    {
        interceptors.Clear();
        hasInterceptor = false;
        localInterceptors.Clear();
        hasLocalInterceptor = false;
    }

    /// <summary>
    /// Returns a derived child router that owns the given filter.
    /// </summary>
    /// <remarks>
    /// The returned router represents the chain <c>parent → ... → this → newFilter</c>.
    /// Publishing directly on the returned router runs the full cumulative chain
    /// before reaching its subscribers — the same way an Rx <c>Where</c> chain
    /// applies every predicate from the source down to the subscription site.
    /// Commands published on the parent also reach subscribers registered on the
    /// child (each filter in the tree is invoked exactly once per publish).
    /// Note: the parent's filter list is snapshotted at this call; subsequent
    /// <c>AddFilter</c> on an ancestor will not retroactively affect existing
    /// children's cumulative chain.
    /// </remarks>
    public Router WithFilter(ICommandInterceptor interceptor)
    {
        var child = new Router();
        child.AttachToParent(this);
        child.AddFilter(interceptor);
        childRouters.Add(child);
        return child;
    }

    // TODO:
    public bool HasInterceptor() => HasFilter();
    public bool HasInterceptor<T>() where T : class, ICommandInterceptor => HasFilter<T>();

    public bool HasFilter() => hasInterceptor;

    public bool HasFilter<T>() where T : class, ICommandInterceptor => FindFilter<T>() != null;

    public bool HasFilter<T>(Func<T, bool> predicate)
        where T : class, ICommandInterceptor =>
        FindFilter(predicate) != null;

    public bool HasFilter<T, TState>(Func<T, TState, bool> predicate, TState state)
        where T : class, ICommandInterceptor =>
        FindFilter(predicate, state) != null;

    public T? FindFilter<T>() where T : class, ICommandInterceptor =>
        FindFilter(x => x is T) as T;

    public T? FindFilter<T>(Func<T, bool> predicate) where T : class, ICommandInterceptor
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull is T x && predicate(x))
            {
                return x;
            }
        }
        return null;
    }

    public T? FindFilter<T, TState>(Func<T, TState, bool> predicate, TState state)
        where T : class, ICommandInterceptor
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull is T x && predicate(x, state))
            {
                return x;
            }
        }
        return null;
    }

    public ICommandInterceptor? FindFilter(Func<ICommandInterceptor, bool> predicate)
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull is { } x && predicate(x))
            {
                return x;
            }
        }
        return null;
    }

    ICommandPublisher ICommandPublisher.WithFilter(ICommandInterceptor interceptor) => WithFilter(interceptor);
    ICommandSubscribable ICommandSubscribable.WithFilter(ICommandInterceptor interceptor) => WithFilter(interceptor);

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            UnsubscribeAll();
            RemoveAllFilters();
            var parent = parentRouter;
            if (parent != null)
            {
                parent.RemoveChildRouter(this);
                parentRouter = null;
            }
        }
    }

    void RemoveChildRouter(Router child)
    {
        childRouters.Remove(child);
    }

    readonly struct PublishCore : IAsyncCommandSubscriber
    {
        readonly Router source;

        public PublishCore(Router source)
        {
            this.source = source;
        }

        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            // var subscribers = source.subscribers.AsSpan();
            var subscribers = source.subscribers.Values;
            for (var i = source.subscribers.LastIndex; i >= 0; i--)
            {
                switch (subscribers[i])
                {
                    case AnonymousSubscriber<T> x: // devirtualization
                        x.ReceiveInternal(command, context);
                        break;
                    case { } x:
                        x.Receive(command, context);
                        break;
                }
            }

            var asyncSubscribersLastIndex = source.asyncSubscribers.LastIndex;
            var childRoutersLastIndex = source.childRouters.LastIndex;
            if (asyncSubscribersLastIndex < 0 && childRoutersLastIndex < 0) return default;

            var whenAll = ContextPool<ReusableWhenAllSource>.Rent();
            whenAll.Reset((asyncSubscribersLastIndex + 1) + (childRoutersLastIndex + 1));

            if (asyncSubscribersLastIndex >= 0)
            {
                var asyncSubscribers = source.asyncSubscribers.Values;
                for (var i = asyncSubscribersLastIndex; i >= 0; i--)
                {
                    switch (asyncSubscribers[i])
                    {
                        case AsyncAnonymousSubscriber<T> x: // Devirtualization
                            whenAll.AddTask(x.ReceiveInternalAsync(command, context));
                            break;
                        case { } x:
                            whenAll.AddTask(x.ReceiveAsync(command, context));
                            break;
                        default:
                            whenAll.IncrementSuccessfully();
                            break;
                    }
                }
            }

            if (childRoutersLastIndex >= 0)
            {
                var children = source.childRouters.Values;
                for (var i = childRoutersLastIndex; i >= 0; i--)
                {
                    if (children[i] is { } child)
                    {
                        whenAll.AddTask(child.FanOutAsync(command, context.CancellationToken));
                    }
                    else
                    {
                        whenAll.IncrementSuccessfully();
                    }
                }
            }

            return new ValueTask(whenAll, whenAll.Version);
        }
    }
}
}