#if VITALROUTER_R3_INTEGRATION
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using VitalRouter.Internal;

namespace VitalRouter.R3;

public static class R3Extensions
{
    public static IDisposable SubscribeToPublish<T>(this Observable<T> source, ICommandPublisher publisher)
        where T : ICommand
    {
        return source.Subscribe(publisher, (x, p) => p.PublishAsync(x).Forget());
    }

    public static UniTask ForEachPublishAndForgetAsync<T>(this Observable<T> source, ICommandPublisher publisher, CancellationToken cancellation = default)
        where T : ICommand
    {
        var observer = new ForEachPublishAndForgetObserver<T>(publisher, cancellation);
        source.Subscribe(observer);
        return observer.Task;
    }

    public static UniTask ForEachPublishAndAwaitAsync<T>(this Observable<T> source, ICommandPublisher publisher, CancellationToken cancellation = default)
        where T : ICommand
    {
        var observer = new ForEachPublishAndAwaitObserver<T>(publisher, cancellation);
        source.Subscribe(observer);
        return observer.Task;
    }

    public static Observable<T> AsObservable<T>(this ICommandSubscribable subscribable) where T : ICommand
    {
        return new CommandSubscriberObservable<T>(subscribable);
    }
}

sealed class CommandSubscriberObservable<T> : Observable<T>, ICommandSubscriber where T : ICommand
{
    Observer<T> observer = default!;
    readonly ICommandSubscribable subscribable;

    public CommandSubscriberObservable(ICommandSubscribable subscribable)
    {
        this.subscribable = subscribable;
    }

    protected override IDisposable SubscribeCore(Observer<T> observer)
    {
        this.observer = observer;
        return subscribable.Subscribe(this);
    }

    public void Receive<TReceive>(TReceive command) where TReceive : ICommand
    {
        if (typeof(TReceive) == typeof(T))
        {
            observer.OnNext(UnsafeHelper.As<TReceive, T>(ref command));
        }
    }
}

sealed class ForEachPublishAndForgetObserver<T> : Observer<T> where T : ICommand
{
    public UniTask Task => outerCompletionSource.Task;

    readonly ICommandPublisher publisher;
    readonly UniTaskCompletionSource outerCompletionSource = new();

    CancellationTokenRegistration tokenRegistration;
    CancellationToken cancellationToken;

    bool isStopped;

    public ForEachPublishAndForgetObserver(ICommandPublisher publisher, CancellationToken cancellationToken)
    {
        this.publisher = publisher;
        this.cancellationToken = cancellationToken;

        if (cancellationToken.CanBeCanceled)
        {
            tokenRegistration = cancellationToken.Register(static state =>
            {
                var s = (ForEachPublishAndForgetObserver<T>)state!;
                s.Dispose(); // observer is subscription, dispose
                s.outerCompletionSource.TrySetCanceled(s.cancellationToken);
            }, this, useSynchronizationContext: false);
        }
    }

    protected override void OnNextCore(T value)
    {
        publisher.PublishAsync(value, cancellationToken).Forget();
    }

    protected override void OnErrorResumeCore(Exception error)
    {
        try
        {
            outerCompletionSource.TrySetException(error);
        }
        finally
        {
            Dispose();
        }
    }

    protected override void OnCompletedCore(Result result)
    {
        if (result.IsFailure)
        {
            try
            {
                outerCompletionSource.TrySetException(result.Exception);
            }
            finally
            {
                Dispose();
            }
        }
        else
        {
            try
            {
                outerCompletionSource.TrySetResult();
            }
            finally
            {
                Dispose();
            }
        }
    }

    // if override, should call base.DisposeCore(), be careful.
    protected override void DisposeCore()
    {
        tokenRegistration.Dispose();
    }
}

sealed class ForEachPublishAndAwaitObserver<T> : Observer<T> where T : ICommand
{
    public UniTask Task => outerCompletionSource.Task;

    readonly ICommandPublisher publisher;
    readonly UniTaskCompletionSource outerCompletionSource = new();
    readonly Queue<T> commandQueue = new();
    readonly Action<object> continuation;

    UniTask.Awaiter currentAwaiter;
    CancellationTokenRegistration tokenRegistration;
    CancellationToken cancellationToken;

    bool isStopped;

    public ForEachPublishAndAwaitObserver(ICommandPublisher publisher, CancellationToken cancellationToken)
    {
        this.publisher = publisher;
        this.cancellationToken = cancellationToken;

        if (cancellationToken.CanBeCanceled)
        {
            tokenRegistration = cancellationToken.Register(static state =>
            {
                var s = (ForEachPublishAndAwaitObserver<T>)state!;
                s.Dispose(); // observer is subscription, dispose
                s.outerCompletionSource.TrySetCanceled(s.cancellationToken);
            }, this, useSynchronizationContext: false);
        }

        continuation = static x => ((ForEachPublishAndAwaitObserver<T>)x).Continue();
    }

    protected override void OnNextCore(T value)
    {
        lock (commandQueue)
        {
            if (commandQueue.Count <= 0)
            {
                currentAwaiter = publisher.PublishAsync(value, cancellationToken).GetAwaiter();
                if (currentAwaiter.IsCompleted)
                {
                    Continue();
                }
                else
                {
                    currentAwaiter.SourceOnCompleted(continuation, this);
                }
            }
            else
            {
                commandQueue.Enqueue(value);
            }
        }
    }

    protected override void OnErrorResumeCore(Exception error)
    {
        try
        {
            outerCompletionSource.TrySetException(error);
        }
        finally
        {
            Dispose();
        }
    }

    protected override void OnCompletedCore(Result result)
    {
        if (result.IsFailure)
        {
            try
            {
                outerCompletionSource.TrySetException(result.Exception);
            }
            finally
            {
                Dispose();
            }
        }
        else
        {
            lock (commandQueue)
            {
                isStopped = true;
                if (commandQueue.Count <= 0)
                {
                    try
                    {
                        outerCompletionSource.TrySetResult();
                    }
                    finally
                    {
                        Dispose();
                    }
                }
            }
        }
    }

    // if override, should call base.DisposeCore(), be careful.
    protected override void DisposeCore()
    {
        tokenRegistration.Dispose();
        currentAwaiter = default;
    }

    void Continue()
    {
        try
        {
            currentAwaiter.GetResult();
        }
        catch (Exception ex)
        {
            outerCompletionSource.TrySetException(ex);
            return;
        }

        lock (commandQueue)
        {
            if (commandQueue.Count > 0)
            {
                var nextCommand = commandQueue.Dequeue();
                currentAwaiter = publisher.PublishAsync(nextCommand, cancellationToken).GetAwaiter();
                if (currentAwaiter.IsCompleted)
                {
                    Continue();
                }
                else
                {
                    currentAwaiter.SourceOnCompleted(continuation, this);
                }
            }
            else
            {
                if (isStopped)
                {
                    try
                    {
                        outerCompletionSource.TrySetResult();
                    }
                    finally
                    {
                        Dispose();
                    }
                }
            }
        }
    }
}
#endif