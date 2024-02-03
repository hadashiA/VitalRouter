using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Tests;

class AInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, ctx);
    }
}

class BInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, ctx);
    }
}

class CInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, ctx);
    }
}

class DInterceptor : ICommandInterceptor
{
    public readonly Queue<ICommand> Receives = new();

    public UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        Receives.Enqueue(command);
        return next(command, ctx);
    }
}

class ThrowInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        throw new TestException();
    }
}

class ErrorHandlingInterceptor : ICommandInterceptor
{
    public Exception? Exception { get; private set; }

    public async UniTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        try
        {
            await next(command, ctx);
        }
        catch (Exception ex)
        {
            Exception = ex;
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
