using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public interface ICommandPublisher
{
    UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default) where T : ICommand;
}

public interface ICommandSubscribable
{
    Subscription Subscribe(ICommandSubscriber subscriber);
    Subscription Subscribe(IAsyncCommandSubscriber subscriber);
}

public interface ICommandSubscriber
{
    void Receive<T>(T command) where T : ICommand;
}

public interface IAsyncCommandSubscriber
{
    UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand;
}

public sealed class CommandBus : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly CommandBus Default = new();

    readonly List<ICommandSubscriber> subscribers = new();
    readonly List<IAsyncCommandSubscriber> asyncSubscribers = new();
    readonly List<IAsyncCommandInterceptor> interceptors = new();

    readonly List<ICommandSubscriber> executingSubscribers = new();
    readonly List<UniTask> executingTasks = new();
    readonly List<IAsyncCommandInterceptor> executingInterceptors = new();

    bool disposed;

    readonly ReusableWhenAllSource whenAllSource = new();
    readonly UniTaskAsyncLock publishLock = new();
    readonly object subscribeLock = new();

    public async UniTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        CheckDispose();

        try
        {
            await publishLock.WaitAsync();

            lock (subscribeLock)
            {
                foreach (var interceptor in interceptors)
                {
                    executingInterceptors.Add(interceptor);
                }
                foreach (var subscriber in subscribers)
                {
                    executingSubscribers.Add(subscriber);
                }
                foreach (var asyncSubscriber in asyncSubscribers)
                {
                    executingTasks.Add(asyncSubscriber.ReceiveAsync(command, cancellation));
                }
            }

            if (executingInterceptors.Count > 0)
            {
                var context = PublishContext<T>.Rent(this, interceptors);
                try
                {
                    await context.InvokeRecursiveAsync(command, cancellation);
                }
                finally
                {
                    context.Return();
                }
                return;
            }

            foreach (var x in executingSubscribers)
            {
                x.Receive(command);
            }

            if (executingTasks.Count > 0)
            {
                whenAllSource.Reset(executingTasks);
                await whenAllSource.Task;
            }
        }
        finally
        {
            publishLock.Release();
            executingTasks.Clear();
            executingSubscribers.Clear();
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
            subscribers.Remove(subscriber);
        }
    }
    public void Unsubscribe(IAsyncCommandSubscriber subscriber)
    {
        lock (subscribeLock)
        {
            asyncSubscribers.Remove(subscriber);
        }
    }

    public void Use(IAsyncCommandInterceptor interceptor)
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
            subscribers.Clear();
        }
    }

    void CheckDispose()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(UniTaskAsyncLock));
        }
    }
}