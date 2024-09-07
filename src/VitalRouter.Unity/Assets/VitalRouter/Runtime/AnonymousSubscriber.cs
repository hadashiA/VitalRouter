using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace VitalRouter
{
public static class SubscribableAnonymousExtensions
{
    public static Subscription Subscribe<T>(this ICommandSubscribable subscribable, Action<T, PublishContext> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AnonymousSubscriber<T>(callback));
    }

    [Obsolete("Use SubscribeAwait instead")]
    public static Subscription Subscribe<T>(
        this ICommandSubscribable subscribable,
        Func<T, PublishContext, UniTask> callback)
        where T : ICommand
    {
        return subscribable.Subscribe(new AsyncAnonymousSubscriber<T>(callback));
    }

    public static Subscription SubscribeAwait<T>(
        this ICommandSubscribable subscribable,
        Func<T, PublishContext, UniTask> callback,
        CommandOrdering? ordering = null)
        where T : ICommand
    {
        return subscribable.Subscribe(new AsyncAnonymousSubscriber<T>(callback, ordering));
    }
}

class AsyncAnonymousSubscriber<T> : IAsyncCommandSubscriber where T : ICommand
{
    Func<T, PublishContext, UniTask> callback;
    readonly ICommandInterceptor? commandOrdering;

    public AsyncAnonymousSubscriber(Func<T, PublishContext, UniTask> callback, CommandOrdering? ordering = null)
    {
        this.callback = callback;
        commandOrdering = ordering switch
        {
            CommandOrdering.Sequential => new SequentialOrdering(),
            CommandOrdering.Drop => new DropOrdering(),
            CommandOrdering.Switch => new SwitchOrdering(),
            _ => null,
        };
    }

    public UniTask ReceiveAsync<TReceive>(TReceive command, PublishContext context) where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            if (commandOrdering != null)
            {
                var c = Unsafe.As<PublishContinuation<TReceive>>(callback);
                return commandOrdering.InvokeAsync(command, context, c);
            }
            var commandCasted = Unsafe.As<TReceive, T>(ref command);
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
            var commandCasted = Unsafe.As<TReceive, T>(ref command);
            callback(commandCasted, context);
        }
    }
}
}
