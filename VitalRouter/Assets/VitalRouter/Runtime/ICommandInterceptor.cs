using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;

namespace VitalRouter;

public interface ICommandInterceptor
{
    UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand;
}

public abstract class TypedCommandInterceptro<T> : ICommandInterceptor
    where T : ICommand
{
    public UniTask InvokeAsync<TReceive>(
        TReceive command,
        CancellationToken cancellation,
        Func<TReceive, CancellationToken, UniTask> next)
        where TReceive : ICommand
    {
        if (command is T x)
        {
            var n = UnsafeUtility.As<
                Func<TReceive, CancellationToken, UniTask>,
                Func<T, CancellationToken, UniTask>
            >(ref next);
            return InvokeAsync(x, cancellation, n);
        }
        return next(command, cancellation);
    }

    public abstract UniTask InvokeAsync(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next);
}