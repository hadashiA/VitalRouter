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

    public async ValueTask PublishAsync<T>(
        T command,
        CancellationToken cancellation = default,
        [CallerMemberName] string? callerMemberName = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        where T : ICommand
    {
        CheckDispose();

        if (HasInterceptor())
        {
            var context = PublishContext<T>.Rent(interceptors, publishCore, cancellation, callerMemberName, callerFilePath, callerLineNumber);
            try
            {
                await context.PublishAsync(command);
            }
            finally
            {
                context.Return();
            }
        }
        else
        {
            var context = PublishContext.Rent(cancellation, callerMemberName, callerFilePath, callerLineNumber);
            try
            {
                await publishCore.ReceiveAsync(command, context);
            }
            finally
            {
                context.Return();
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

    public Router Filter(ICommandInterceptor interceptor)
    {
        interceptors.Add(interceptor);
        return this;
    }

    public void RemoveFilter(Func<ICommandInterceptor, bool> predicate)
    {
        var span = interceptors.AsSpan();
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (interceptors[i] is { } x && predicate(x))
            {
                interceptors.RemoveAt(i);
            }
        }
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

    public bool HasInterceptor()
    {
        foreach (var interceptorOrNull in interceptors.AsSpan())
        {
            if (interceptorOrNull != null)
            {
                return true;
            }
        }
        return false;
    }

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
            try
            {
                foreach (var sub in source.subscribers.AsSpan())
                {
                    sub?.Receive(command, context);
                }

                foreach (var sub in source.asyncSubscribers.AsSpan())
                {
                    if (sub != null)
                    {
                        var task = sub.ReceiveAsync(command, context);
                        executingTasks.Add(task);
                    }
                }

                if (executingTasks.Count > 0)
                {
                    return WhenAllUtility.WhenAll(executingTasks);
                }

                return default;
            }
            finally
            {
                executingTasks.Clear();
            }
        }
    }
}
}
