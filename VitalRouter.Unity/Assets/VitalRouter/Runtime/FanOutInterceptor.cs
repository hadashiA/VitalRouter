using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public class FanOutInterceptor : ICommandInterceptor
{
    readonly List<ICommandPublisher> subsequents = new();
    readonly ReusableWhenAllSource whenAllSource = new();
    readonly ExpandBuffer<UniTask> executingTasks = new(4);

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

        try
        {
            foreach (var x in subsequents)
            {
                executingTasks.Add(x.PublishAsync(command, cancellation));
            }

            whenAllSource.Reset(executingTasks);
            await whenAllSource.Task;
        }
        finally
        {
            executingTasks.Clear();
        }
    }
}