using System;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

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
    /// If commands are published simultaneously, wait until the subscriber has processed the first command.
    /// </summary>
    [Obsolete("Use CommandOrdering.Sequential instead.")]
    FirstInFirstOut,
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
            case CommandOrdering.FirstInFirstOut:
                Filter(new FirstInFirstOutOrdering());
                break;
        }
        return this;
    }
}

public class SequentialOrdering : ICommandInterceptor, IDisposable
{
    readonly UniTaskAsyncLock publishLock = new();

    public async UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
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

[Obsolete("Use SequentialOrdering instead.")]
public class FirstInFirstOutOrdering : ICommandInterceptor, IDisposable
{
    readonly UniTaskAsyncLock publishLock = new();

    public async UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
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
