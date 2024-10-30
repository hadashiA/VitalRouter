using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public interface ICommandPublisher
{
    ValueTask PublishAsync<T>(
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand;
}

public interface ICommandSubscribable
{
    Subscription Subscribe(ICommandSubscriber subscriber);
    Subscription Subscribe(IAsyncCommandSubscriber subscriber);
    void Unsubscribe(ICommandSubscriber subscriber);
    void Unsubscribe(IAsyncCommandSubscriber subscriber);
}

public interface ICommandSubscriber
{
    void Receive<T>(T command, PublishContext context) where T : ICommand;
}

public interface IAsyncCommandSubscriber
{
    ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand;
}

public static class CommandPublisherExtensions
{
    static readonly Dictionary<Type, MethodInfo> PublishMethods = new();
    static MethodInfo? publishMethodOpenGeneric;

    public static ValueTask PublishAsync(
        this ICommandPublisher publisher,
        Type commandType,
        object command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        MethodInfo publishMethod;
        lock (publisher)
        {
            if (!PublishMethods.TryGetValue(commandType, out publishMethod))
            {
                publishMethodOpenGeneric ??= typeof(ICommandPublisher).GetMethod("PublishAsync", BindingFlags.Instance | BindingFlags.Public);
                var typeArguments = CappedArrayPool<Type>.Shared8Limit.Rent(1);
                typeArguments[0] = commandType;
                publishMethod = publishMethodOpenGeneric!.MakeGenericMethod(typeArguments);
                PublishMethods.Add(commandType, publishMethod);
                CappedArrayPool<Type>.Shared8Limit.Return(typeArguments);
            }
        }

        var args = CappedArrayPool<object?>.Shared8Limit.Rent(5);
        args[0] = command;
        args[1] = cancellation;
        args[2] = callerMemberName;
        args[3] = callerFilePath;
        args[4] = callerLineNumber;
        var result = publishMethod.Invoke(publisher, args);
        CappedArrayPool<object?>.Shared8Limit.Return(args);
        return (ValueTask)result;
    }

    public static void Enqueue<T>(
        this ICommandPublisher publisher,
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand
    {
        publisher.PublishAsync(command, cancellation, callerMemberName, callerFilePath, callerLineNumber);
    }

    public static void Enqueue(
        this ICommandPublisher publisher,
        Type commandType,
        object command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        publisher.PublishAsync(commandType, command, cancellation, callerMemberName, callerFilePath, callerLineNumber);
    }
}

public sealed partial class Router : ICommandPublisher, ICommandSubscribable, IDisposable
{
    public static readonly Router Default = new();

    readonly FreeList<ICommandSubscriber> subscribers = new(8);
    readonly FreeList<IAsyncCommandSubscriber> asyncSubscribers = new(8);
    readonly FreeList<ICommandInterceptor> interceptors = new(8);

    bool disposed;
    bool hasInterceptor;

    readonly PublishCore publishCore;

#if VITALROUTER_VCONTAINER_INTEGRATION
    [global::VContainer.Inject]
#endif
    public Router()
    {
        publishCore = new PublishCore(this);
    }

    public Router(CommandOrdering ordering) : this()
    {
        Filter(ordering);
    }

    public ValueTask PublishAsync<T>(
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand
    {
        CheckDispose();

        ValueTask task;
        PublishContext context = default!;
        try
        {
            if (HasInterceptor())
            {
                var c = PublishContext<T>.Rent(interceptors, publishCore, cancellation, callerMemberName, callerFilePath, callerLineNumber);
                context = c;
                task = c.PublishAsync(command);
            }
            else
            {
                context = PublishContext.Rent(cancellation, callerMemberName, callerFilePath, callerLineNumber);
                task = publishCore.ReceiveAsync(command, context!);
            }
        }
        finally
        {
            context.Return();
        }


        if (task.IsCompletedSuccessfully)
        {
            return task;
        }

        return ContinueAsync(task, context);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        async ValueTask ContinueAsync(ValueTask x, PublishContext c)
        {
            try
            {
                await x;
            }
            finally
            {
                c.Return();
            }
        }
    }

    public Subscription Subscribe(ICommandSubscriber subscriber)
    {
        subscribers.Add(subscriber);
        return new Subscription(this, subscriber);
    }

    public Subscription Subscribe(IAsyncCommandSubscriber subscriber)
    {
        asyncSubscribers.Add(subscriber);
        return new Subscription(this, subscriber);
    }

    public void Unsubscribe(ICommandSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    public void Unsubscribe(IAsyncCommandSubscriber subscriber)
    {
        asyncSubscribers.Remove(subscriber);
    }

    public void UnsubscribeAll()
    {
        subscribers.Clear();
        asyncSubscribers.Clear();
    }

    [Obsolete("Use AddFilter instead")]
    public Router Filter(ICommandInterceptor interceptor)
    {
        AddFilter(interceptor);
        return this;
    }

    public void AddFilter(ICommandInterceptor interceptor)
    {
        hasInterceptor = true;
        interceptors.Add(interceptor);
    }

    public void RemoveFilter(Func<ICommandInterceptor, bool> predicate)
    {
        var span = interceptors.AsSpan();
        var count = 0;
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (interceptors[i] is { } x)
            {
                count++;
                if (predicate(x))
                {
                    interceptors.RemoveAt(i);
                    count--;
                }
            }
        }
        hasInterceptor = count > 0;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            subscribers.Clear();
            asyncSubscribers.Clear();
            interceptors.Clear();
        }
    }

    public bool HasInterceptor<T>() where T : ICommandInterceptor
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull is T)
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasInterceptor() => hasInterceptor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckDispose()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(Router));
        }
    }

    class PublishCore : IAsyncCommandSubscriber
    {
        readonly Router source;
        readonly ExpandBuffer<ValueTask> executingTasks = new(8);

        public PublishCore(Router source)
        {
            this.source = source;
        }

        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            foreach (var sub in source.subscribers.AsSpan())
            {
                switch (sub)
                {
                    case AnonymousSubscriber<T> x: // Optimize devirtualization
                        x.ReceiveInternal(command, context);
                        break;
                    case { } x:
                        x.Receive(command, context);
                        break;
                }
            }

            executingTasks.Clear();
            foreach (var sub in source.asyncSubscribers.AsSpan())
            {
                var task = sub?.ReceiveAsync(command, context);
                if (task != null)
                {
                    executingTasks.Add(task.Value);
                }
            }

            if (executingTasks.Count > 0)
            {
                return WhenAllUtility.WhenAll(executingTasks);
            }

            return default;
        }
    }
}
}