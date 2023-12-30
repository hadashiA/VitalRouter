using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

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

    readonly ExpandBuffer<ICommandSubscriber> subscribers = new(8);
    readonly ExpandBuffer<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly ExpandBuffer<IAsyncCommandInterceptor> interceptors = new(4);

    readonly ExpandBuffer<ICommandSubscriber> executingSubscribers = new(8);
    readonly ExpandBuffer<UniTask> executingTasks = new(8);
    readonly ExpandBuffer<IAsyncCommandInterceptor> executingInterceptors = new(4);

    bool disposed;

    readonly ReusableWhenAllSource whenAllSource = new();
    readonly UniTaskAsyncLock publishLock = new();

    readonly object subscribeLock = new();
    SpinLock interceptorsLock;

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
                var context = PublishContext<T>.Rent(this, executingInterceptors);
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
                await PublishCoreAsync(command, cancellation);
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
            subscribers.Clear(true);
            executingTasks.Clear(true);
            executingSubscribers.Clear(true);
            executingInterceptors.Clear(true);
        }
    }

    internal UniTask PublishCoreAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        try
        {
            lock (subscribeLock)
            {
                subscribers.CopyAndSetLengthTo(executingSubscribers);
                for (var i = 0; i < asyncSubscribers.Count; i++)
                {
                    executingTasks.Add(asyncSubscribers[i].ReceiveAsync(command, cancellation));
                }
            }

            for (var i = 0; i < executingSubscribers.Count; i++)
            {
                executingSubscribers[i].Receive(command);
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
            executingTasks.Clear(true);
            executingSubscribers.Clear(true);
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