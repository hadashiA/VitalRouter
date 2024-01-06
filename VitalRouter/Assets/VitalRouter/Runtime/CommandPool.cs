using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter;

public interface IPoolableCommand : ICommand
{
    void OnReturnToPool();
}

public interface ICommandPool<T> where T : IPoolableCommand
{
    T Rent(Func<T> factory);
    T Rent<TArg1>(Func<TArg1, T> factory, TArg1 arg1);
    T Rent<TArg1, TArg2>(Func<TArg1, TArg2, T> factory, TArg1 arg1, TArg2 arg2);
    T Rent<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3);
    T Rent<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    T Rent<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    void Return(T command);
}

public static class CommandPool<T> where T : IPoolableCommand
{
    public static readonly ICommandPool<T> Shared = new ConcurrentQueueCommandPool<T>();
}

public class ConcurrentQueueCommandPool<T> : ICommandPool<T> where T : IPoolableCommand
{
    readonly ConcurrentQueue<T> queue = new();

    public T Rent(Func<T> factory)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory();
    }

    public T Rent<TArg1>(Func<TArg1, T> factory, TArg1 arg1)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory(arg1);
    }

    public T Rent<TArg1, TArg2>(Func<TArg1, TArg2, T> factory, TArg1 arg1, TArg2 arg2)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory(arg1, arg2);
    }

    public T Rent<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory(arg1, arg2, arg3);
    }

    public T Rent<TArg1, TArg2, TArg3, TArg4>(Func<TArg1, TArg2, TArg3, TArg4, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory(arg1, arg2, arg3, arg4);
    }

    public T Rent<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> factory, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        if (queue.TryDequeue(out var value))
        {
            return value;
        }
        return factory(arg1, arg2, arg3, arg4, arg5);
    }

    public void Return(T command)
    {
        command.OnReturnToPool();
        queue.Enqueue(command);
    }
}

public class CommandPooling : ICommandInterceptor
{
    public async UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        try
        {
            await next(command, cancellation);
        }
        finally
        {
            if (command is IPoolableCommand poolable)
            {
                Return(poolable);
            }
        }
    }

    static void Return<T>(T command) where T : IPoolableCommand
    {
        CommandPool<T>.Shared.Return(command);
    }
}