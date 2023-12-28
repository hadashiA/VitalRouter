using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public interface ICommandPublisher
{
    void Publish<T>(T msg) where T : ICommand;
    UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default) where T : ICommand;
}

public interface ICommandSubscribable
{
    Subscription Subscribe(ICommandSubscriber subscriber);
}

public interface ICommandSubscriber
{
}

public interface IImmediateCommandSubscriber : ICommandSubscriber
{
    void Receive<T>(T command) where T : ICommand;
}

public interface IAsyncCommandSubscriber : ICommandSubscriber
{
    UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand;
}

public sealed class CommandBus : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly CommandBus Default = new();

    readonly List<ICommandSubscriber> subscribers = new();
    readonly List<ICommandInterceptor> interceptors = new();

    readonly List<UniTask> executingTasks = new();
    readonly List<IImmediateCommandSubscriber> executingImmediateTasks = new();

    bool disposed;

    readonly ReusableWhenAllSource whenAllSource = new();
    readonly UniTaskAsyncLock publishLock = new();
    readonly object subscribeLock = new();

    public void Publish<T>(T command) where T : ICommand
    {
        // TODO: specific implementation
        PublishAsync(command).Forget();
    }

    public async UniTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        CheckDispose();

        try
        {
            await publishLock.WaitAsync();

            if (interceptors.Count > 0)
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

            executingTasks.Clear();
            executingImmediateTasks.Clear();

            lock (subscribeLock)
            {
                foreach (var subscriber in subscribers)
                {
                    switch (subscriber)
                    {
                        case IAsyncCommandSubscriber x:
                            executingTasks.Add(x.ReceiveAsync(command, cancellation));
                            break;
                        case IImmediateCommandSubscriber x:
                            executingImmediateTasks.Add(x);
                            break;
                    }
                }
            }

            foreach (var immediate in executingImmediateTasks)
            {
                immediate.Receive(command);
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
        }
    }

    public Subscription Subscribe(ICommandSubscriber subscriber)
    {
        lock (subscribers)
        {
            subscribers.Add(subscriber);
        }
        return new Subscription(this, subscriber);
    }

    public void Unsubscribe(ICommandSubscriber subscriber)
    {
        lock (subscribers)
        {
            subscribers.Remove(subscriber);
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