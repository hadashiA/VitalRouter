using System.Collections.Generic;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public class FanOutInterceptor : ICommandInterceptor
{
    readonly List<ICommandPublisher> subsequents = new();
    readonly ExpandBuffer<ValueTask> executingTasks = new(4);

    public void Add(ICommandPublisher publisher)
    {
        subsequents.Add(publisher);
    }

    public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
        where T : ICommand
    {
        await next(command, context);
        try
        {
            var whenAll = ContextPool<ReusableWhenAllSource>.Rent();
            whenAll.Reset(subsequents.Count);

            foreach (var x in subsequents)
            {
                var awaiter = x.PublishAsync(
                    command,
                    context.CancellationToken,
                    context.CallerMemberName,
                    context.CallerFilePath,
                    context.CallerLineNumber).GetAwaiter();
                whenAll.AddAwaiter(awaiter);
            }

            await new ValueTask(whenAll, whenAll.Version);
        }
        finally
        {
            executingTasks.Clear();
        }
    }
}
}