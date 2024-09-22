#if VITALROUTER_UNITASK_INTEGRATION
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Internal
{
    static class WhenAllUtility
    {
        public static ValueTask WhenAll(IReadOnlyList<ValueTask> tasks)
        {
            if (tasks.Count <= 0) return default;
            if (tasks.Count <= 1) return tasks[0];

            var uniTasks = ArrayPool<UniTask>.Shared.Rent(tasks.Count);
            try
            {
                for (var i = 0; i < tasks.Count; ++i)
                {
                    uniTasks[i] = tasks[i].AsUniTask();
                }
                return ReusableWhenAllSource.WhenAll(uniTasks);
            }
            finally
            {
                ArrayPool<UniTask>.Shared.Return(uniTasks);
            }
        }
    }

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

        public static UniTask WhenAll(IReadOnlyList<UniTask> tasks)
        {
            if (!pool.TryDequeue(out var source))
            {
                source = new ReusableWhenAllSource();
            }

            source.Reset(tasks);
            return source.Task;
        }

        class AwaiterState
        {
            public ReusableWhenAllSource Source = default!;
            public UniTask.Awaiter Awaiter;
        }

        void Reset(IReadOnlyList<UniTask> tasks)
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
}
#else
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace VitalRouter.Internal
{
    static class ContinuationSentinel
    {
        public static readonly Action<object?> AvailableContinuation = _ => { };
        public static readonly Action<object?> CompletedContinuation = _ => { };
    }

    static class WhenAllUtility
    {
        public static ValueTask WhenAll(IReadOnlyList<ValueTask> tasks)
        {
            return new ValueTask(new WhenAllPromiseAll(tasks), 0);
        }

        class WhenAllPromiseAll : IValueTaskSource
        {
            static readonly ContextCallback execContextCallback = ExecutionContextCallback;
            static readonly SendOrPostCallback syncContextCallback = SynchronizationContextCallback;

            int taskCount;
            int completedCount;
            ExceptionDispatchInfo? exception;
            Action<object?> continuation = ContinuationSentinel.AvailableContinuation;
            Action<object?>? invokeContinuation;
            object? state;
            SynchronizationContext? syncContext;
            ExecutionContext? execContext;

            public WhenAllPromiseAll(IReadOnlyList<ValueTask> tasks)
            {
                taskCount = tasks.Count;

                for (var i = 0; i < taskCount; i++)
                {
                    var awaiter = tasks[i].GetAwaiter();
                    if (awaiter.IsCompleted)
                    {
                        try
                        {
                            awaiter.GetResult();
                        }
                        catch (Exception ex)
                        {
                            exception = ExceptionDispatchInfo.Capture(ex);
                            return;
                        }
                        TryInvokeContinuationWithIncrement();
                    }
                    else
                    {
                        RegisterContinuation(awaiter, i);
                    }
                }
            }

            void RegisterContinuation(ValueTaskAwaiter awaiter, int index)
            {
                awaiter.UnsafeOnCompleted(() =>
                {
                    try
                    {
                        awaiter.GetResult();
                    }
                    catch (Exception ex)
                    {
                        exception = ExceptionDispatchInfo.Capture(ex);
                        TryInvokeContinuation();
                        return;
                    }
                    TryInvokeContinuationWithIncrement();
                });
            }

            void TryInvokeContinuationWithIncrement()
            {
                if (Interlocked.Increment(ref completedCount) == taskCount)
                {
                    TryInvokeContinuation();
                }
            }

            void TryInvokeContinuation()
            {
                var c = Interlocked.Exchange(ref continuation, ContinuationSentinel.CompletedContinuation);
                if (c != ContinuationSentinel.AvailableContinuation && c != ContinuationSentinel.CompletedContinuation)
                {
                    var spinWait = new SpinWait();
                    while (state == null) // worst case, state is not set yet so wait.
                    {
                        spinWait.SpinOnce();
                    }

                    if (execContext != null)
                    {
                        invokeContinuation = c;
                        ExecutionContext.Run(execContext, execContextCallback, this);
                    }
                    else if (syncContext != null)
                    {
                        invokeContinuation = c;
                        syncContext.Post(syncContextCallback, this);
                    }
                    else
                    {
                        c(state);
                    }
                }
            }

            public void GetResult(short token)
            {
                if (exception != null)
                {
                    exception.Throw();
                }
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                return (completedCount == taskCount) ? ValueTaskSourceStatus.Succeeded
                    : (exception != null) ? ((exception.SourceException is OperationCanceledException) ? ValueTaskSourceStatus.Canceled : ValueTaskSourceStatus.Faulted)
                    : ValueTaskSourceStatus.Pending;
            }

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                var c = Interlocked.CompareExchange(ref this.continuation, continuation, ContinuationSentinel.AvailableContinuation);
                if (c == ContinuationSentinel.CompletedContinuation)
                {
                    continuation(state);
                    return;
                }

                if (c != ContinuationSentinel.AvailableContinuation)
                {
                    throw new InvalidOperationException("does not allow multiple await.");
                }

                if (state == null)
                {
                    throw new InvalidOperationException("invalid state.");
                }

                if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) == ValueTaskSourceOnCompletedFlags.FlowExecutionContext)
                {
                    execContext = ExecutionContext.Capture();
                }
                if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) == ValueTaskSourceOnCompletedFlags.UseSchedulingContext)
                {
                    syncContext = SynchronizationContext.Current;
                }
                this.state = state;

                if (GetStatus(token) != ValueTaskSourceStatus.Pending)
                {
                    TryInvokeContinuation();
                }
            }

            static void ExecutionContextCallback(object state)
            {
                var self = (WhenAllPromiseAll)state;
                if (self.syncContext != null)
                {
                    self.syncContext.Post(syncContextCallback, self);
                }
                else
                {
                    var invokeContinuation = self.invokeContinuation!;
                    var invokeState = self.state;
                    self.invokeContinuation = null;
                    self.state = null;
                    invokeContinuation(invokeState);
                }
            }

            static void SynchronizationContextCallback(object state)
            {
                var self = (WhenAllPromiseAll)state;
                var invokeContinuation = self.invokeContinuation!;
                var invokeState = self.state;
                self.invokeContinuation = null;
                self.state = null;
                invokeContinuation(invokeState);
            }
        }

    }
}
#endif