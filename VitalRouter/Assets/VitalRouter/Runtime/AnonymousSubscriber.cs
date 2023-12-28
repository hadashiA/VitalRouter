using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public static class SubscribableAnonymousExtensions
{
    public static Subscription Subscribe<T>(this ICommandSubscribable subscribable, Action<T> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AnonymousSubscriber<T>(callback));
    }

    public static Subscription Subscribe<T>(
        this ICommandSubscribable subscribable,
        Func<T, CancellationToken, UniTask> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AsyncAnonymousSubscriber<T>(callback));
    }
}

class AsyncAnonymousSubscriber<T> : IAsyncCommandSubscriber where T : ICommand
{
    readonly Func<T, CancellationToken, UniTask> callback;

    public AsyncAnonymousSubscriber(Func<T, CancellationToken, UniTask> callback)
    {
        this.callback = callback;
    }

    public UniTask ReceiveAsync<TReceive>(TReceive command, CancellationToken cancellation = default)
        where TReceive : ICommand
    {
        if (command is T x)
        {
            return callback(x, cancellation);
        }
        return UniTask.CompletedTask;
    }
}

class AnonymousSubscriber<T> : ICommandSubscriber where T : ICommand
{
    readonly Action<T> callback;

    public AnonymousSubscriber(Action<T> callback)
    {
        this.callback = callback;
    }

    public void Receive<TReceive>(TReceive command) where TReceive : ICommand
    {
        if (command is T x)
        {
           callback(x);
        }
    }
}