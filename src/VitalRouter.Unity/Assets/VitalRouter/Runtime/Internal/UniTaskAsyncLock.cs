#if VITALROUTER_UNITASK_INTEGRATION
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal
{
class UniTaskAsyncLock : IDisposable
{
    readonly Queue<AutoResetUniTaskCompletionSource> asyncWaitingQueue = new();
    readonly object syncRoot = new();
    readonly int maxResourceCount = 1;

    int currentResourceCount = 1;
    int waitCount;
    int countOfWaitersPulsedToWake;
    bool disposed;

    public UniTask WaitAsync()
    {
        CheckDispose();
        lock (syncRoot)
        {
            if (currentResourceCount > 0)
            {
                currentResourceCount--;
                return UniTask.CompletedTask;
            }

            var source = AutoResetUniTaskCompletionSource.Create();
            asyncWaitingQueue.Enqueue(source);
            return source.Task;
        }
    }

    public void Release()
    {
        CheckDispose();

        var releaseCount = 1;
        lock (syncRoot)
        {
            var currentResourceCountLocal = currentResourceCount;
            if (maxResourceCount - currentResourceCountLocal < releaseCount)
            {
                throw new InvalidOperationException();
            }

            currentResourceCountLocal += releaseCount;

            // Signal to any synchronous waiters
            var waitCountLocal = waitCount;
            var waitersToNotify = Math.Min(currentResourceCountLocal, waitCountLocal) - countOfWaitersPulsedToWake;
            if (waitersToNotify > 0)
            {
                if (waitersToNotify > releaseCount)
                {
                    waitersToNotify = releaseCount;
                }

                countOfWaitersPulsedToWake += waitersToNotify;
                for (var i = 0; i < waitersToNotify; i++)
                {
                    Monitor.Pulse(syncRoot);
                }
            }

            var maxAsyncToRelease = currentResourceCountLocal;
            for (var i = 0; i < maxAsyncToRelease; i++)
            {
                if (asyncWaitingQueue.TryDequeue(out var waitingTask))
                {
                    --currentResourceCountLocal;
                    waitingTask.TrySetResult();
                }
            }
            currentResourceCount = currentResourceCountLocal;
        }
    }

    public void Dispose()
    {
        disposed = true;
        while (asyncWaitingQueue.TryDequeue(out var waitingTask))
        {
            waitingTask.TrySetCanceled();
        }
    }

    void CheckDispose()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(UniTaskAsyncLock));
        }
    }
}
}
#endif