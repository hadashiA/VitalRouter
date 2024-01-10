using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter;

public class FirstInFirstOutOrdering : ICommandInterceptor, IDisposable
{
    public static readonly FirstInFirstOutOrdering Instance = new();

    readonly UniTaskAsyncLock publishLock = new();

    public async UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        await publishLock.WaitAsync();
        try
        {
            await next(command, cancellation);
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

public static class RouterCommandOrderingExtensions
{
    public static Router FirstInFirstOut(this Router router)
    {
        return router.Filter(FirstInFirstOutOrdering.Instance);
    }
}