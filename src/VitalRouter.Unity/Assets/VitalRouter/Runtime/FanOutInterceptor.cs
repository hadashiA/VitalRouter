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
            foreach (var x in subsequents)
            {
                executingTasks.Add(x.PublishAsync(command, context.CancellationToken, context.CallerMemberName, context.CallerFilePath, context.CallerLineNumber));
            }

            await WhenAllUtility.WhenAll(executingTasks);
        }
        finally
        {
            executingTasks.Clear();
        }
    }
}
}