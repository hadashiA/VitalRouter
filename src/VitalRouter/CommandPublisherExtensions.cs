using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter.Internal;

namespace VitalRouter
{
public static class CommandPublisherExtensions
{
    static readonly Dictionary<Type, MethodInfo> PublishMethods = new();
    static MethodInfo? publishMethodOpenGeneric;

    public static ValueTask PublishAsync(
        this ICommandPublisher publisher,
        Type commandType,
        object command,
        CancellationToken cancellation = default)
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
        var result = publishMethod.Invoke(publisher, args);
        CappedArrayPool<object?>.Shared8Limit.Return(args);
        return (ValueTask)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enqueue<T>(this ICommandPublisher publisher, T command, CancellationToken cancellation = default)
        where T : ICommand
    {
        publisher.PublishAsync(command, cancellation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enqueue(
        this ICommandPublisher publisher,
        Type commandType,
        object command,
        CancellationToken cancellation = default)
    {
        publisher.PublishAsync(commandType, command, cancellation);
    }
}
}
