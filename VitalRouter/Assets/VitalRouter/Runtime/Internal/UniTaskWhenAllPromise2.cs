// using System;
// using System.Threading;
// using Cysharp.Threading.Tasks;
//
// namespace VitalRouter;
//
// sealed class WhenAllPromise2 : IUniTaskSource
// {
//     int completeCount;
//     int tasksLength;
//     UniTaskCompletionSourceCore<AsyncUnit> core; // don't reset(called after GetResult, will invoke TrySetException.)
//
//     public WhenAllPromise2(UniTask[] tasks, int tasksLength)
//     {
//         TaskTracker.TrackActiveTask(this, 3);
//
//         this.tasksLength = tasksLength;
//         this.completeCount = 0;
//
//         if (tasksLength == 0)
//         {
//             core.TrySetResult(AsyncUnit.Default);
//             return;
//         }
//
//         for (var i = 0; i < tasksLength; i++)
//         {
//             UniTask.Awaiter awaiter;
//             try
//             {
//                 awaiter = tasks[i].GetAwaiter();
//             }
//             catch (Exception ex)
//             {
//                 core.TrySetException(ex);
//                 continue;
//             }
//
//             if (awaiter.IsCompleted)
//             {
//                 TryInvokeContinuation(this, awaiter);
//             }
//             else
//             {
//                 awaiter.SourceOnCompleted(state =>
//                 {
//                     using (var t = (StateTuple<WhenAllPromise, UniTask.Awaiter>)state)
//                     {
//                         TryInvokeContinuation(t.Item1, t.Item2);
//                     }
//                 }, StateTuple.Create(this, awaiter));
//             }
//         }
//     }
//
//     static void TryInvokeContinuation(WhenAllPromise self, in UniTask.Awaiter awaiter)
//     {
//         try
//         {
//             awaiter.GetResult();
//         }
//         catch (Exception ex)
//         {
//             self.core.TrySetException(ex);
//             return;
//         }
//
//         if (Interlocked.Increment(ref self.completeCount) == self.tasksLength)
//         {
//             self.core.TrySetResult(AsyncUnit.Default);
//         }
//     }
//
//     public void GetResult(short token)
//     {
//         TaskTracker.RemoveTracking(this);
//         GC.SuppressFinalize(this);
//         core.GetResult(token);
//     }
//
//     public UniTaskStatus GetStatus(short token)
//     {
//         return core.GetStatus(token);
//     }
//
//     public UniTaskStatus UnsafeGetStatus()
//     {
//         return core.UnsafeGetStatus();
//     }
//
//     public void OnCompleted(Action<object> continuation, object state, short token)
//     {
//         core.OnCompleted(continuation, state, token);
//     }
// }