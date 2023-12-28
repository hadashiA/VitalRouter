using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

// class AnonymousInterceptor : IAsyncCommandInterceptor
// {
//     readonly Func<ICommand, CancellationToken, Func<ICommand, CancellationToken, UniTask>, UniTask> callback;
//
//     public AnonymousInterceptor(Func<T, CancellationToken, Func<T, CancellationToken, UniTask>, UniTask> callback)
//     {
//         this.callback = callback;
//     }
//
//     public UniTask InvokeAsync<TReceive>(
//         TReceive command,
//         CancellationToken cancellation,
//         Func<TReceive, CancellationToken, UniTask> next)
//         where TReceive : ICommand
//     {
//         return callback(x, cancellation, next);
//     }
// }