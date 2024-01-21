using System;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public static class SubscribableAnonymousExtensions
{
    public static Subscription Subscribe<T>(this ICommandSubscribable subscribable, Action<T, PublishContext> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AnonymousSubscriber<T>(callback));
    }

    public static Subscription Subscribe<T>(
        this ICommandSubscribable subscribable,
        Func<T, PublishContext, UniTask> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AsyncAnonymousSubscriber<T>(callback));
    }
}

class AsyncAnonymousSubscriber<T> : IAsyncCommandSubscriber where T : ICommand
{
    readonly Func<T, PublishContext, UniTask> callback;

    public AsyncAnonymousSubscriber(Func<T, PublishContext, UniTask> callback)
    {
        this.callback = callback;
    }

    public UniTask ReceiveAsync<TReceive>(TReceive command, PublishContext context) where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            var commandCasted = UnsafeHelper.As<TReceive, T>(ref command);
            return callback(commandCasted, context);
        }
        return UniTask.CompletedTask;
    }
}

class AnonymousSubscriber<T> : ICommandSubscriber where T : ICommand
{
    readonly Action<T, PublishContext> callback;

    public AnonymousSubscriber(Action<T, PublishContext> callback)
    {
        this.callback = callback;
    }

    public void Receive<TReceive>(TReceive command, PublishContext context) where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            var commandCasted = UnsafeHelper.As<TReceive, T>(ref command);
            callback(commandCasted, context);
        }
    }
}
