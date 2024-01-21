using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public interface ICommandPublisher
{
    UniTask PublishAsync<T>(
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand;
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
    UniTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand;
}

public static class CommandPublisherExtensions
{
    public static void Enqueue<T>(
        this ICommandPublisher publisher,
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand
    {
        publisher.PublishAsync(command, cancellation, callerMemberName, callerFilePath, callerLineNumber).Forget();
    }
}

public sealed partial class Router : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly Router Default = new();

    readonly FreeList<ICommandSubscriber> subscribers = new(8);
    readonly FreeList<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly FreeList<ICommandInterceptor> interceptors = new(8);

    bool disposed;

    readonly PublishCore publishCore;

    public Router(CommandOrdering ordering = CommandOrdering.Parallel)
    {
        publishCore = new PublishCore(this);
        Filter(ordering);
    }

    public async UniTask PublishAsync<T>(
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand
    {
        CheckDispose();

        if (HasInterceptor())
        {
            var context = PublishContext<T>.Rent(interceptors, publishCore, cancellation, callerMemberName, callerFilePath, callerLineNumber);
            try
            {
                await context.PublishAsync(command);
            }
            finally
            {
                context.Return();
            }
        }
        else
        {
            var context = PublishContext.Rent(cancellation, callerMemberName, callerFilePath, callerLineNumber);
            try
            {
                await publishCore.ReceiveAsync(command, context);
            }
            finally
            {
                context.Return();
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
        interceptors.Clear();
    }

    public Router Filter(ICommandInterceptor interceptor)
    {
        interceptors.Add(interceptor);
        return this;
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

    bool HasInterceptor()
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull != null)
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
            throw new ObjectDisposedException(nameof(UniTaskAsyncLock));
        }
    }

    class PublishCore : IAsyncCommandSubscriber
    {
        readonly Router source;
        readonly ReusableWhenAllSource whenAllSource = new();
        readonly ExpandBuffer<UniTask> executingTasks = new(8);

        public PublishCore(Router source)
        {
            this.source = source;
        }

        public UniTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            try
            {
                foreach (var sub in source.subscribers.AsSpan())
                {
                    sub?.Receive(command, context);
                }

                foreach (var sub in source.asyncSubscribers.AsSpan())
                {
                    if (sub != null)
                    {
                        var task = sub.ReceiveAsync(command, context);
                        executingTasks.Add(task);
                    }
                }

                if (executingTasks.Count > 0)
                {
                    whenAllSource.Reset(executingTasks);
                    return whenAllSource.Task;
                }

                return UniTask.CompletedTask;
            }
            finally
            {
                executingTasks.Clear();
            }
        }
    }
}