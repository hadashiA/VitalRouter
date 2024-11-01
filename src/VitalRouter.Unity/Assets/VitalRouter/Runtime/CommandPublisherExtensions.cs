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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}
