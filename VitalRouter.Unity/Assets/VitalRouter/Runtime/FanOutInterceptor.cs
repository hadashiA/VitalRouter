using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public class FanOutInterceptor : ICommandInterceptor
{
    readonly List<ICommandPublisher> subsequents = new();

    public void Add(ICommandPublisher publisher)
    {
        subsequents.Add(publisher);
    }

    public async UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation, Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        await next(command, cancellation);

        foreach (var x in subsequents)
        {
            x.PublishAsync(command, cancellation).Forget();
        }
    }
}