using System;
using System.Threading;
using System.Threading.Tasks;

namespace VitalRouter;

public interface IAsyncLock  : IDisposable
{
    ValueTask WaitAsync(CancellationToken cancellationToken = default);
    void Release();
}

public class SemaphoreSlimAsyncLock : IAsyncLock
{
    readonly SemaphoreSlim semaphore = new(1, 1);

    public async ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
    }

    public void Release()
    {
        semaphore.Release();
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }
}