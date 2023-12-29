using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public interface IAsyncCommandInterceptor
{
    UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand;

    // UniTask InvokeAsync<T, TResult>(
    //     T command,
    //     CancellationToken cancellation,
    //     Func<TResult, CancellationToken, UniTask> next)
    //     where T : ICommand
    //     where TResult : ICommand;
}
