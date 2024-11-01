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
}

public interface ICommandSubscribable
{
    Subscription Subscribe(ICommandSubscriber subscriber);
    Subscription Subscribe(IAsyncCommandSubscriber subscriber);
    void Unsubscribe(ICommandSubscriber subscriber);
    void Unsubscribe(IAsyncCommandSubscriber subscriber);
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

    readonly FreeList<ICommandSubscriber> subscribers = new(8);
    readonly FreeList<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly FreeList<ICommandInterceptor> interceptors = new(8);

    bool disposed;
    bool hasInterceptor;

    readonly PublishCore publishCore;

#if VITALROUTER_VCONTAINER_INTEGRATION
    [global::VContainer.Inject]
#endif
    public Router()
    {
        publishCore = new PublishCore(this);
    }

    public Router(CommandOrdering ordering) : this()
    {
        AddFilter(ordering);
    }

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
    public ValueTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        CheckDispose();

        ValueTask task;
        PublishContext context;
        if (hasInterceptor)
        {
            var c = PublishContext<T>.Rent(interceptors, publishCore, cancellation);
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

    [Obsolete("Use AddFilter instead")]
    public Router Filter(ICommandInterceptor interceptor)
    {
        AddFilter(interceptor);
        return this;
    }

    public void AddFilter(ICommandInterceptor interceptor)
    {
        hasInterceptor = true;
        interceptors.Add(interceptor);
    }

    public void RemoveFilter(Func<ICommandInterceptor, bool> predicate)
    {
        var span = interceptors.AsSpan();
        var count = 0;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (interceptors[i] is { } x)
            {
                count++;
                if (predicate(x))
                {
                    interceptors.RemoveAt(i);
                    count--;
                }
            }
        }
        hasInterceptor = count > 0;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            subscribers.Clear();
            asyncSubscribers.Clear();
            interceptors.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasInterceptor() => hasInterceptor;

    public bool HasInterceptor<T>() where T : ICommandInterceptor
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull is T)
            {
                return true;
            }
        }
        return false;
    }

    void CheckDispose()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(Router));
        }
    }

    class PublishCore : IAsyncCommandSubscriber
    {
        readonly Router source;

        public PublishCore(Router source)
        {
            this.source = source;
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            foreach (var sub in source.subscribers.AsSpan())
            {
                switch (sub)
                {
                    case AnonymousSubscriber<T> x: // Optimize devirtualization
                        x.ReceiveInternal(command, context);
                        break;
                    case { } x:
                        x.Receive(command, context);
                        break;
                }
            }

            if (source.asyncSubscribers.IsEmpty) return default;
            var asyncSubscribers = source.asyncSubscribers.AsSpan();

            var whenAll = ContextPool<ReusableWhenAllSource>.Rent();
            whenAll.Reset(asyncSubscribers.Length);
            foreach (var sub in asyncSubscribers)
            {
                switch (sub)
                {
                    case AsyncAnonymousSubscriber<T> x: // Devirtualization
                        whenAll.AddAwaiter(x.ReceiveInternalAsync(command, context).GetAwaiter());
                        break;
                    case { } x:
                        whenAll.AddAwaiter(x.ReceiveAsync(command, context).GetAwaiter());
                        break;
                    default:
                        whenAll.IncrementSuccessfully();
                        break;
                }
            }
            return new ValueTask(whenAll, whenAll.Version);
        }
    }
}
}