using System;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public enum CommandOrdering
{
    /// <summary>
    /// If commands are published simultaneously, subscribers are called in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// If commands are published simultaneously, wait until the subscriber has processed the first command.
    /// </summary>
    Sequential,

    /// <summary>
    /// If commands are published simultaneously, ignore commands that come later.
    /// </summary>
    Drop,

    /// <summary>
    /// If the previous asynchronous method is running, it is cancelled and the next asynchronous method is executed.
    /// </summary>
    Switch,
}

public partial class Router
{
    [Obsolete("Use AddFilter instead")]
    public Router Filter(CommandOrdering ordering)
    {
        AddFilter(ordering);
        return this;
    }

    public void AddFilter(CommandOrdering ordering)
    {
        switch (ordering)
        {
            case CommandOrdering.Sequential:
                AddFilter(new SequentialOrdering());
                break;
            case CommandOrdering.Parallel:
                break;
            case CommandOrdering.Drop:
                AddFilter(new DropOrdering());
                break;
            case CommandOrdering.Switch:
                AddFilter(new SwitchOrdering());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ordering), ordering, null);
        }
    }
}

public class SequentialOrdering : ICommandInterceptor, IDisposable
{
#if VITALROUTER_UNITASK_INTEGRATION
    readonly UniTaskAsyncLock publishLock = new();
#else
    readonly SemaphoreSlim publishLock = new(1, 1);
#endif

    public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
        where T : ICommand
    {
        await publishLock.WaitAsync();
        try
        {
            await next(command, context);
        }
        finally
        {
            publishLock.Release();
        }
    }

    public void Dispose()
    {
        publishLock.Dispose();
    }
}

public class DropOrdering : ICommandInterceptor
{
    int executingCount;

    public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        if (Interlocked.CompareExchange(ref executingCount, 1, 0) == 0)
        {
            try
            {
                await next(command, context);
            }
            finally
            {
                Interlocked.Exchange(ref executingCount, 0);
            }
        }
    }
}

public class SwitchOrdering : ICommandInterceptor
{
    CancellationTokenSource? previousCancellationSource;

    public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        previousCancellationSource?.Cancel();
        previousCancellationSource?.Dispose();
        previousCancellationSource = new CancellationTokenSource();

        context.CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
            previousCancellationSource.Token,
            context.CancellationToken
            ).Token;
        return next(command, context);
    }
}
}
