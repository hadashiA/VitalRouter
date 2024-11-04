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

    sealed class PublishCore : IAsyncCommandSubscriber
    {
        readonly Router source;

        public PublishCore(Router source)
        {
            this.source = source;
        }

        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            if (!source.subscribers.IsEmpty)
            {
                foreach (var sub in source.subscribers.Values.AsSpan(source.subscribers.LastIndex + 1))
                {
                    if (sub == null) continue;
                    ref var x = ref Unsafe.AsRef(in sub);
                    if (Unsafe.As<ICommandSubscriber, nint>(ref x) == AnonymousSubscriber<T>.TypeHandleValue)
                    {
                        Unsafe.As<ICommandSubscriber, AnonymousSubscriber<T>>(ref x).ReceiveInternal(command, context);
                    }
                    else
                    {
                        x.Receive(command, context);
                    }
                }
            }

            if (!source.asyncSubscribers.IsEmpty)
            {
                var asyncSubscribers = source.asyncSubscribers.Values.AsSpan(source.asyncSubscribers.LastIndex + 1);

                var whenAll = ContextPool<ReusableWhenAllSource>.Rent();
                whenAll.Reset(asyncSubscribers.Length);
                foreach (var sub in asyncSubscribers)
                {
                    if (sub == null)
                    {
                        whenAll.IncrementSuccessfully();
                        continue;
                    }

                    ValueTask task;
                    ref var x = ref Unsafe.AsRef(in sub);
                    if (Unsafe.As<IAsyncCommandSubscriber, uint>(ref x) == AsyncAnonymousSubscriber<T>.TypeHandleValue)
                    {
                        task = Unsafe.As<IAsyncCommandSubscriber, AsyncAnonymousSubscriber<T>>(ref x)
                            .ReceiveInternalAsync(command, context);
                        whenAll.AddAwaiter(task.GetAwaiter());
                    }
                    else
                    {
                        task = x.ReceiveAsync(command, context);
                    }
                    whenAll.AddAwaiter(x.ReceiveAsync(command, context).GetAwaiter());
                }
                return new ValueTask(whenAll, whenAll.Version);
            }
            return default;
        }
    }
}
}