using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            // This additional amount of spinwaiting in addition
            // to Monitor.Enter()'s spinwaiting has shown measurable perf gains in test scenarios.
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
            // If the count > 0 we are good to move on.
            // If not, then wait if we were given allowed some wait duration

            if (currentResourceCount == 0)
            {
                waitSuccessful = Monitor.Wait(syncRoot, Timeout.Infinite);
            }

            // Now try to acquire.  We prioritize acquisition over cancellation/timeout so that we don't
            // lose any counts when there are asynchronous waiters in the mix.  Asynchronous waiters
            // defer to synchronous waiters in priority, which means that if it's possible an asynchronous
            // waiter didn't get released because a synchronous waiter was present, we need to ensure
            // that synchronous waiter succeeds so that they have a chance to release.
            Debug.Assert(!waitSuccessful || currentResourceCount > 0,
                "If the wait was successful, there should be count available.");
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
            // Read the m_currentCount into a local variable to avoid unnecessary volatile accesses inside the lock.
            var count = currentResourceCount;

            // If the release count would result exceeding the maximum count, throw SemaphoreFullException.
            if (maxResourceCount - count < releaseCount)
            {
                throw new InvalidOperationException();
            }

            // Increment the count by the actual release count
            count += releaseCount;

            // Signal to any synchronous waiters, taking into account how many waiters have previously been pulsed to wake
            // but have not yet woken
            var waitCount = this.waitCount;
            var waitersToNotify = Math.Min(count, waitCount) - countOfWaitersPulsedToWake;
            if (waitersToNotify > 0)
            {
                // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but
                // since WaitUntilCountOrTimeout() does not have enough information to tell whether a woken thread was
                // pulsed, it's possible for m_countOfWaitersPulsedToWake to be less than the number of threads that have
                // actually been pulsed to wake.
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

            // Now signal to any asynchronous waiters, if there are any.  While we've already
            // signaled the synchronous waiters, we still hold the lock, and thus
            // they won't have had an opportunity to acquire this yet.  So, when releasing
            // asynchronous waiters, we assume that all synchronous waiters will eventually
            // acquire the semaphore.  That could be a faulty assumption if those synchronous
            // waits are canceled, but the wait code path will handle that.

            var maxAsyncToRelease = count;
            for (var i = 0; i < maxAsyncToRelease; i++)
            {
                if (asyncWaitingQueue.TryDequeue(out var waitingTask))
                {
                    --count;
                    waitingTask.TrySetResult();
                }
            }
            currentResourceCount = count;
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