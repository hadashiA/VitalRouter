using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public interface ICommandPublisher
{
    UniTask PublishAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand;
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
    void Receive<T>(T command) where T : ICommand;
}

public interface IAsyncCommandSubscriber
{
    UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand;
}

public static class CommandPublisherExtensions
{
    public static void Enqueue<T>(
        this ICommandPublisher publisher,
        T command,
        CancellationToken cancellation = default)
        where T : ICommand
    {
        publisher.PublishAsync(command, cancellation).Forget();
    }
}

public sealed partial class Router : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly Router Default = new();

    readonly FreeList<ICommandSubscriber> subscribers = new(8);
    readonly FreeList<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly FreeList<ICommandInterceptor> interceptors = new(8);

    bool disposed;

    readonly ReusableWhenAllSource whenAllSource = new();
    readonly object subscribeLock = new();
    readonly PublishCore publishCore;

    public Router()
    {
        publishCore = new PublishCore(this);
    }

    public UniTask PublishAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
    {
        CheckDispose();

        var hasInterceptor = false;
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull != null)
            {
                hasInterceptor = true;
                break;
            }
        }
        if (hasInterceptor)
        {
            var context = InvokeContextWithFreeList<T>.Rent(interceptors, publishCore);
            try
            {
                return context.InvokeRecursiveAsync(command, cancellation);
            }
            finally
            {
                context.Return();
            }
        }
        return publishCore.InvokeAsync(command, cancellation, null!);
    }

    public Subscription Subscribe(ICommandSubscriber subscriber)
    {
        subscribers.Add(subscriber);
        return new Subscription(this, subscriber);
    }

    public Subscription Subscribe(IAsyncCommandSubscriber subscriber)
    {
        lock (subscribeLock)
        {
            asyncSubscribers.Add(subscriber);
        }

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

    void CheckDispose()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(UniTaskAsyncLock));
        }
    }

    class PublishCore : ICommandInterceptor
    {
        readonly Router source;
        readonly ExpandBuffer<UniTask> executingTasks;

        public PublishCore(Router source)
        {
            this.source = source;
            executingTasks = new ExpandBuffer<UniTask>(8);
        }

        public UniTask InvokeAsync<T>(
            T command,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> _)
            where T : ICommand
        {
            try
            {
                for (var i = 0; i <= source.subscribers.LastIndex; i++)
                {
                    source.subscribers[i]?.Receive(command);
                }
                for (var i = 0; i <= source.asyncSubscribers.LastIndex; i++)
                {
                    if (source.asyncSubscribers[i] is { } asyncSubscriber)
                    {
                        var task = asyncSubscriber.ReceiveAsync(command, cancellation);
                        executingTasks.Add(task);
                    }
                }

                if (executingTasks.Count > 0)
                {
                    source.whenAllSource.Reset(executingTasks);
                    return source.whenAllSource.Task;
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