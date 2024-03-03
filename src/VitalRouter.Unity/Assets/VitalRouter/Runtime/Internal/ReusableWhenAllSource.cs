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

    static readonly ConcurrentQueue<ReusableWhenAllSource> pool = new();
    static readonly ConcurrentQueue<AwaiterState> statePool = new();

    public static UniTask WhenAllAsync(IReadOnlyList<UniTask> tasks)
    {
        if (!pool.TryDequeue(out var source))
        {
            source = new ReusableWhenAllSource();
        }

        source.Reset(tasks);
        return new UniTask(source, source.core.Version);
    }

    class AwaiterState
    {
        public ReusableWhenAllSource Source = default!;
        public UniTask.Awaiter Awaiter;
    }

    internal void Reset(IReadOnlyList<UniTask> tasks)
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

                awaiter.SourceOnCompleted(x =>
                {
                    var xs = (AwaiterState)x;
                    try
                    {
                        xs.Source.TryInvokeContinuation(in xs.Awaiter);
                    }
                    finally
                    {
                        statePool.Enqueue(xs);
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
            pool.Enqueue(this);
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