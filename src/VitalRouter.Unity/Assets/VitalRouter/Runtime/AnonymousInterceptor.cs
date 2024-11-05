using System;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public static class CommandBusAnonymousExtensions
{
    public static void AddFilter<T>(
        this Router router,
        Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback)
        where T : ICommand
    {
        router.AddFilter(new AnonymousInterceptor<T>(callback));
    }

    public static Router WithFilter<T>(
        this Router router,
        Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback)
        where T : ICommand
    {
        return router.WithFilter(new AnonymousInterceptor<T>(callback));
    }

    public static ICommandPublisher WithFilter<T>(
        this ICommandPublisher publihser,
        Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback)
        where T : ICommand
    {
        return publihser.WithFilter(new AnonymousInterceptor<T>(callback));
    }

    public static ICommandSubscribable WithFilter<T>(
        this ICommandSubscribable subscribable,
        Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback)
        where T : ICommand
    {
        return subscribable.WithFilter(new AnonymousInterceptor<T>(callback));
    }
}

sealed class AnonymousInterceptor<T> : ICommandInterceptor where T : ICommand
{
    readonly Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback;

    public AnonymousInterceptor(Func<T, PublishContext, PublishContinuation<T>, ValueTask> callback)
    {
        this.callback = callback;
    }

    public ValueTask InvokeAsync<TReceive>(
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
}
