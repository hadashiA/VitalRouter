using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal;

sealed class ReusableWhenAllSource : IUniTaskSource
{
    public UniTask Task => new(this, core.Version);

    int completeCount;
    int tasksLength;
    UniTaskCompletionSourceCore<AsyncUnit> core; // don't reset(called after GetResult, will invoke TrySetException.)
    SpinLock spinLock;

    readonly List<Exception> exceptions = new();

    static readonly ConcurrentQueue<AwaiterState> statePool = new();

    class AwaiterState
    {
        public ReusableWhenAllSource Source;
        public UniTask.Awaiter Awaiter;
    }

    public void Reset(IReadOnlyList<UniTask> tasks)
    {
        core.Reset();
        exceptions.Clear();
        completeCount = 0;
        tasksLength = tasks.Count;

        if (tasksLength == 0)
        {
            core.TrySetResult(AsyncUnit.Default);
            return;
        }

        for (var i = 0; i < tasksLength; i++)
        {
            UniTask.Awaiter awaiter;
            try
            {
                awaiter = tasks[i].GetAwaiter();
            }
            catch (Exception ex)
            {
                AddException(ex);
                continue;
            }

            if (awaiter.IsCompleted)
            {
                TryInvokeContinuation(in awaiter);
            }
            else
            {
                if (!statePool.TryDequeue(out var state))
                {
                    state = new AwaiterState();
                }
                state.Source = this;
                state.Awaiter = awaiter;

                awaiter.SourceOnCompleted(state =>
                {
                    var x = (AwaiterState)state;
                    try
                    {
                        x.Source.TryInvokeContinuation(in x.Awaiter);
                    }
                    finally
                    {
                        statePool.Enqueue(x);
                    }
                }, state);
            }
        }
    }

    public void GetResult(short token)
    {
        GC.SuppressFinalize(this);
        core.GetResult(token);
    }

    public UniTaskStatus GetStatus(short token)
    {
        return core.GetStatus(token);
    }

    public UniTaskStatus UnsafeGetStatus()
    {
        return core.UnsafeGetStatus();
    }

    public void OnCompleted(Action<object> continuation, object state, short token)
    {
        core.OnCompleted(continuation, state, token);
    }

    void TryInvokeContinuation(in UniTask.Awaiter awaiter)
    {
        try
        {
            awaiter.GetResult();
        }
        catch (Exception ex)
        {
            AddException(ex);
        }

        if (Interlocked.Increment(ref completeCount) == tasksLength)
        {
            if (exceptions.Count > 0)
            {
                core.TrySetException(new AggregateException(exceptions));
            }
            else
            {
                core.TrySetResult(AsyncUnit.Default);
            }
        }
    }

    void AddException(Exception exception)
    {
        var lockTaken = false;
        try
        {
            spinLock.Enter(ref lockTaken);
            exceptions.Add(exception);
        }
        finally
        {
            if (lockTaken) spinLock.Exit();
        }
    }
}