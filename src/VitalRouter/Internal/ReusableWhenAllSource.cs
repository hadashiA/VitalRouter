using System;
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

    class ReusableWhenAllSource : IValueTaskSource
    {
        class AwaiterNode
        {
            ReusableWhenAllSource parent = default!;
            ValueTaskAwaiter awaiter;

            readonly Action continuation;

            public AwaiterNode()
            {
                continuation = OnCompleted;
            }

            public static void RegisterUnsafeOnCompleted(ReusableWhenAllSource parent, ValueTaskAwaiter awaiter)
            {
                var node = ContextPool<AwaiterNode>.Rent();
                node.parent = parent;
                node.awaiter = awaiter;
                node.awaiter.UnsafeOnCompleted(node.continuation);
            }

            void OnCompleted()
            {
                var p = parent;
                var a = awaiter;
                parent = null!;
                awaiter = default;

                ContextPool<AwaiterNode>.Return(this);
                try
                {
                    a.GetResult();
                    p.IncrementSuccessfully();
                }
                catch (Exception ex)
                {
                    p.error = ExceptionDispatchInfo.Capture(ex);
                    p.TryInvokeContinuation();
                }
            }
        }

        static readonly ContextCallback ExecContextCallback = ExecutionContextCallback;
        static readonly SendOrPostCallback SyncContextCallback = SynchronizationContextCallback;

        public short Version => version;

        Action<object?> continuation = ContinuationSentinel.AvailableContinuation;
        Action<object?>? invokeContinuation;
        object? continuationState;
        ExecutionContext? executionContext;
        SynchronizationContext? synchronizationContext;
        ExceptionDispatchInfo? error;
        int taskCount;
        int completedCount;
        short version;

        public static ReusableWhenAllSource Run(ReadOnlySpan<ValueTask?> tasks)
        {
            var source = ContextPool<ReusableWhenAllSource>.Rent();
            source.Reset(tasks.Length);

            foreach (var task in tasks)
            {
                if (task is { } t)
                {
                    source.AddTask(t);
                }
                else
                {
                    source.IncrementSuccessfully();
                }
            }
            return source;
        }

        public void Reset(int taskCount)
        {
            // Reset/update state for the next use/await of this instance.
            if (++version == short.MaxValue) version = 0;
            this.taskCount = taskCount;
            completedCount = 0;
            error = null;
            executionContext = null;
            synchronizationContext = null;
            continuation = ContinuationSentinel.AvailableContinuation;
            continuationState = null;
        }

        public void AddTask(ValueTask task)
        {
            if (task.IsCompletedSuccessfully)
            {
                IncrementSuccessfully();
            }
            else if (task.IsFaulted)
            {
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    error = ExceptionDispatchInfo.Capture(ex);
                }
            }
            else
            {
                AwaiterNode.RegisterUnsafeOnCompleted(this, task.GetAwaiter());
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (completedCount == taskCount)
            {
                return ValueTaskSourceStatus.Succeeded;
            }
            if (error != null)
            {
                return error.SourceException is OperationCanceledException
                    ? ValueTaskSourceStatus.Canceled
                    : ValueTaskSourceStatus.Faulted;
            }
            return ValueTaskSourceStatus.Pending;
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

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                executionContext = ExecutionContext.Capture();
            }
            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                synchronizationContext = SynchronizationContext.Current;
            }
            continuationState = state;

            if (GetStatus(token) != ValueTaskSourceStatus.Pending)
            {
                TryInvokeContinuation();
            }
        }

        public void GetResult(short token)
        {
            try
            {
                if (version != token)
                {
                    throw new InvalidOperationException($"Invalid operation. Expected token {version} but was {token}.");
                }
                error?.Throw();
            }
            finally
            {
                ContextPool<ReusableWhenAllSource>.Return(this);
            }
        }

        public void IncrementSuccessfully()
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
                // var spinWait = new SpinWait();
                // while (continuationState == null) // worst case, state is not set yet so wait.
                // {
                //     spinWait.SpinOnce();
                // }

                if (executionContext != null)
                {
                    invokeContinuation = c;
                    ExecutionContext.Run(executionContext, ExecContextCallback, this);
                }
                else if (synchronizationContext != null)
                {
                    invokeContinuation = c;
                    synchronizationContext.Post(SyncContextCallback, this);
                }
                else
                {
                    c(continuationState);
                }
            }
        }

        static void ExecutionContextCallback(object? state)
        {
            var self = (ReusableWhenAllSource)state!;
            if (self.synchronizationContext != null)
            {
                self.synchronizationContext.Post(SyncContextCallback, self);
            }
            else
            {
                var invokeContinuation = self.invokeContinuation!;
                var invokeState = self.continuationState;
                self.invokeContinuation = null;
                self.continuationState = null;
                invokeContinuation(invokeState);
            }
        }

        static void SynchronizationContextCallback(object? state)
        {
            var self = (ReusableWhenAllSource)state!;
            var invokeContinuation = self.invokeContinuation!;
            var invokeState = self.continuationState;
            self.invokeContinuation = null;
            self.continuationState = null;
            invokeContinuation(invokeState);
        }
    }
}