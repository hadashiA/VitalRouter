using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public interface ICommandPublisher
{
    UniTask PublishAsync<T>(T msg, CancellationToken cancellation = default)
        where T : ICommand;
}

// public interface ICommandSubscriber
// {
//     IDisposable Subscribe<T>(IAsyncCommandListener );
// }

public interface ICommandListener
{
    void Execute<T>(T command) where T : ICommand;
}

public interface IAsyncCommandListener
{
    UniTask ExecuteAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand;
}

public sealed class CommandBus : ICommandPublisher, IDisposable
{
    public static readonly CommandBus Default = new();

    readonly List<IAsyncCommandListener> asyncListeners = new();
    readonly List<ICommandListener> listeners = new();
    readonly List<UniTask> executingTasks = new();

    long subscribing;
    bool disposed;

    readonly object subscribeLock = new();
    readonly UniTaskAsyncLock publishLock = new();

    ~CommandBus()
    {
        Dispose(false);
    }

    public void Publish<T>(T command) where T : ICommand
    {
        // TODO: performance
        PublishAsync(command).Forget();
    }

    public async UniTask PublishAsync<T>(T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        CheckDispose();

        List<Exception>? exceptions = null;
        executingTasks.Clear();
        try
        {
            await publishLock.WaitAsync();

            lock (subscribeLock)
            {
                foreach (var listener in asyncListeners)
                {
                    executingTasks.Add(listener.ExecuteAsync(command, cancellation));
                }
            }

            foreach (var task in executingTasks)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    (exceptions ??= new List<Exception>()).Add(ex);
                }
            }

            foreach (var listener in listeners)
            {
                try
                {
                    listener.Execute(command);
                }
                catch (Exception ex)
                {
                    (exceptions ??= new List<Exception>()).Add(ex);
                }
            }
        }
        finally
        {
            publishLock.Release();
        }

        if (exceptions != null)
        {
            throw new AggregateException(exceptions);
        }
    }

    public Subscription Subscribe(ICommandListener listener)
    {
        lock (subscribeLock)
        {
            listeners.Add(listener);
        }
        return new Subscription(this, listener);
    }

    public Subscription Subscribe(IAsyncCommandListener listener)
    {
        lock (subscribeLock)
        {
            asyncListeners.Add(listener);
        }
        return new Subscription(this, listener);
    }


    public void Unsubscribe(ICommandListener listener)
    {
        lock (subscribeLock)
        {
            listeners.Remove(listener);
        }
    }

    public void Unsubscribe(IAsyncCommandListener listener)
    {
        lock (subscribeLock)
        {
            asyncListeners.Remove(listener);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (!disposed)
        {
            disposed = true;

            if (disposing)
            {
                publishLock.Dispose();
                asyncListeners.Clear();
                listeners.Clear();
            }
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