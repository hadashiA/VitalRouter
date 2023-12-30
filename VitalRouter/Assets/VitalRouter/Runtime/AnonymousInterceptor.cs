using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;

namespace VitalRouter;

public static class CommandBusAnonymousExtensions
{
    public static void Use<T>(
        this CommandBus commandBus,
        Func<T, CancellationToken, Func<T, CancellationToken, UniTask>, UniTask> callback)
        where T : ICommand
    {
        commandBus.Use(new AnonymousInterceptor<T>(callback));
    }
}

class AnonymousInterceptor<T> : ICommandInterceptor where T : ICommand
{
    readonly Func<T, CancellationToken, Func<T, CancellationToken, UniTask>, UniTask> callback;

    public AnonymousInterceptor(Func<T, CancellationToken, Func<T, CancellationToken, UniTask>, UniTask> callback)
    {
        this.callback = callback;
    }

    public UniTask InvokeAsync<TReceive>(
        TReceive command,
        CancellationToken cancellation,
        Func<TReceive, CancellationToken, UniTask> next)
        where TReceive : ICommand
    {
        if (command is T x)
        {
            var y = UnsafeUtility.As<
                Func<TReceive, CancellationToken, UniTask>,
                Func<T, CancellationToken, UniTask>>(ref next);
            return callback(x, cancellation, y);
        }
        else
        {
            return next(command, cancellation);
        }
    }
}
