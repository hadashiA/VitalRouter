using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal;

class UniTaskAsyncLock : IDisposable
{
    readonly Queue<AutoResetUniTaskCompletionSource> asyncWaitingQueue = new();
    readonly object syncRoot = new();
    readonly int maxResourceCount = 1;

    int currentResourceCount = 1;
    int waitCount;
    int countOfWaitersPulsedToWake;
    bool disposed;

    public void Wait()
    {
        CheckDispose();
        var waitSuccessful = false;
        var lockTaken = false;

        try
        {
            // Perf: first spin wait for the count to be positive.
            if (currentResourceCount == 0)
            {
                // Monitor.Enter followed by Monitor.Wait is much more expensive than waiting on an event as it involves another
                // spin, contention, etc. The usual number of spin iterations that would otherwise be used here is increased to
                // lessen that extra expense of doing a proper wait.
                // var spinCount = SpinWait.SpinCountforSpinBeforeWait * 4;
                var spinCount = 35 * 4;
                SpinWait spinner = default;
                while (spinner.Count < spinCount)
                {
                    spinner.SpinOnce();
                    if (currentResourceCount != 0)
                    {
                        break;
                    }
                }
            }

            // Fallback to monitor
            Monitor.Enter(syncRoot, ref lockTaken);
            waitCount++;

            // Wait
            if (currentResourceCount == 0)
            {
                waitSuccessful = Monitor.Wait(syncRoot, Timeout.Infinite);
            }

            // // acquired
            // Debug.Assert(!waitSuccessful || currentResourceCount > 0,
            //     "If the wait was successful, there should be count available.");
            if (currentResourceCount > 0)
            {
                currentResourceCount--;
            }
        }
        finally
        {
            // Release the lock
            if (lockTaken)
            {
                waitCount--;
                Monitor.Exit(syncRoot);
            }
        }
    }

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