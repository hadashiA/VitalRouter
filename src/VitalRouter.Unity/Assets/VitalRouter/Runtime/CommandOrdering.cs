using System;
using System.Threading;
using System.Threading.Tasks;

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
    public Router Filter(CommandOrdering ordering)
    {
        switch (ordering)
        {
            case CommandOrdering.Sequential:
                Filter(new SequentialOrdering());
                break;
            case CommandOrdering.Parallel:
                break;
            case CommandOrdering.Drop:
                Filter(new DropOrdering());
                break;
            case CommandOrdering.Switch:
                Filter(new SwitchOrdering());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ordering), ordering, null);
        }
        return this;
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
    readonly CancellationToken[] tokensBuffer = new CancellationToken[2];

    public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        previousCancellationSource?.Cancel();
        previousCancellationSource?.Dispose();
        previousCancellationSource = new CancellationTokenSource();
        tokensBuffer[0] = previousCancellationSource.Token;
        tokensBuffer[1] = context.CancellationToken;
        context.CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(tokensBuffer).Token;
        return next(command, context);
    }
}
}
