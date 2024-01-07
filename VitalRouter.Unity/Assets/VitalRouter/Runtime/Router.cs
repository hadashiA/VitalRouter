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

    // public ICommandPool CommandPool { get; set; } = ConcurrentQueueCommandPool.Shared;

    readonly ExpandBuffer<ICommandSubscriber> subscribers = new(8);
    readonly ExpandBuffer<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly ExpandBuffer<ICommandInterceptor> interceptors = new(4);

    readonly ExpandBuffer<ICommandSubscriber> executingSubscribers = new(8);
    readonly ExpandBuffer<UniTask> executingTasks = new(8);
    readonly ExpandBuffer<ICommandInterceptor> executingInterceptors = new(4);

    bool disposed;

    readonly ReusableWhenAllSource whenAllSource = new();
    readonly UniTaskAsyncLock publishLock = new();
    readonly object subscribeLock = new();

    readonly ICommandInterceptor publishCore;

    public Router()
    {
        publishCore = new PublishCore(this);
    }

    public async UniTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        CheckDispose();

        try
        {
            await publishLock.WaitAsync();

            lock (subscribeLock)
            {
                if (interceptors.Count > 0)
                {
                    interceptors.CopyAndSetLengthTo(executingInterceptors);
                }
            }

            if (executingInterceptors.Count > 0)
            {
                executingInterceptors.Add(publishCore);
                var context = InvokeContext<T>.Rent(executingInterceptors);
                try
                {
                    await context.InvokeRecursiveAsync(command, cancellation);
                }
                finally
                {
                    context.Return();
                }
            }
            else
            {
                await publishCore.InvokeAsync(command, cancellation, null!);
            }
        }
        finally
        {
            executingInterceptors.Clear(true);
            publishLock.Release();
        }
    }

    public Subscription Subscribe(ICommandSubscriber subscriber)
    {
        lock (subscribeLock)
        {
            subscribers.Add(subscriber);
        }

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
        lock (subscribeLock)
        {
            var i = subscribers.IndexOf(subscriber);
            if (i >= 0)
            {
                subscribers.RemoveAt(i);
            }
        }
    }

    public void Unsubscribe(IAsyncCommandSubscriber subscriber)
    {
        lock (subscribeLock)
        {
            var i = asyncSubscribers.IndexOf(subscriber);
            if (i >= 0)
            {
                asyncSubscribers.RemoveAt(i);
            }
        }
    }

    public void UnsubscribeAll()
    {
        lock (subscribeLock)
        {
            subscribers.Clear(true);
            asyncSubscribers.Clear(true);
        }
    }

    public void Use(ICommandInterceptor interceptor)
    {
        lock (subscribers)
        {
            interceptors.Add(interceptor);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            publishLock.Dispose();
            subscribers.Clear(true);
            asyncSubscribers.Clear(true);
            executingTasks.Clear(true);
            executingSubscribers.Clear(true);
            executingInterceptors.Clear(true);
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

        public PublishCore(Router source)
        {
            this.source = source;
        }

        public UniTask InvokeAsync<T>(
            T command,
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> _)
            where T : ICommand
        {
            try
            {
                lock (source.subscribeLock)
                {
                    source.subscribers.CopyAndSetLengthTo(source.executingSubscribers);
                    for (var i = 0; i < source.asyncSubscribers.Count; i++)
                    {
                        source.executingTasks.Add(source.asyncSubscribers[i].ReceiveAsync(command, cancellation));
                    }
                }

                for (var i = 0; i < source.executingSubscribers.Count; i++)
                {
                    source.executingSubscribers[i].Receive(command);
                }

                if (source.executingTasks.Count > 0)
                {
                    source.whenAllSource.Reset(source.executingTasks);
                    return source.whenAllSource.Task;
                }

                return UniTask.CompletedTask;
            }
            finally
            {
                source.executingTasks.Clear(true);
                source.executingSubscribers.Clear(true);
            }
        }
    }
}