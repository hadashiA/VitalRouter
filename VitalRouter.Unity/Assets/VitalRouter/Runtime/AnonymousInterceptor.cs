using System;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public static class CommandBusAnonymousExtensions
{
    public static void Filter<T>(
        this Router router,
        Func<T, PublishContext, PublishContinuation<T>, UniTask> callback)
        where T : ICommand
    {
        router.Filter(new AnonymousInterceptor<T>(callback));
    }
}

class AnonymousInterceptor<T> : ICommandInterceptor where T : ICommand
{
    readonly Func<T, PublishContext, PublishContinuation<T>, UniTask> callback;

    public AnonymousInterceptor(Func<T, PublishContext, PublishContinuation<T>, UniTask> callback)
    {
        this.callback = callback;
    }

    public UniTask InvokeAsync<TReceive>(
        TReceive command,
        PublishContext context,
        PublishContinuation<TReceive> next)
        where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            var commandCasted = UnsafeHelper.As<TReceive, T>(ref command);
            var nextCasted = UnsafeHelper.As<PublishContinuation<TReceive>, PublishContinuation<T>>(ref next);
            return callback(commandCasted, context, nextCasted);
        }
        return next(command, context);
    }
}
