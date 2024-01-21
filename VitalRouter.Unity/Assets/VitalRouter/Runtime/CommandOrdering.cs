using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public enum CommandOrdering
{
    Parallel,
    FirstInFirstOut,
}

public partial class Router
{
    public Router Filter(CommandOrdering ordering)
    {
        switch (ordering)
        {
            case CommandOrdering.FirstInFirstOut:
                Filter(FirstInFirstOutOrdering.Instance);
                break;
        }
        return this;
    }
}

public class FirstInFirstOutOrdering : ICommandInterceptor, IDisposable
{
    public static readonly FirstInFirstOutOrdering Instance = new();

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
