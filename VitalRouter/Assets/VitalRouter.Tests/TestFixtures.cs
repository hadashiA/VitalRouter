using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Tests;

class AInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, cancellation);
    }
}

class BInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, cancellation);
    }
}

class CInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, cancellation);
    }
}

class DInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, cancellation);
    }
}

class ThrowInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(T command, CancellationToken cancellation, Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        throw new TestException();
    }
}

class ErrorHandlingInterceptor : ICommandInterceptor
{
    public Exception Exception { get; private set; }

    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        try
        {
            return next(command, cancellation);
        }
        catch (Exception ex)
        {
            Exception = ex;
            return UniTask.CompletedTask;
        }
    }
}

class TestException : Exception
{
}

class CommandA : ICommand
{
    public int Value { get; }

    public CommandA(int value)
    {
        Value = value;
    }
}

class CommandB : ICommand
{
    public int Value { get; }

    public CommandB(int value)
    {
        Value = value;
    }
}

class CommandC : ICommand
{
    public int Value { get; }

    public CommandC(int value)
    {
        Value = value;
    }
}

class CommandD : ICommand
{
    public int Value { get; }

    public CommandD(int value)
    {
        Value = value;
    }
}
