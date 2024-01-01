using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.Tests;

class AInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        throw new NotImplementedException();
    }
}

public class BInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        throw new NotImplementedException();
    }
}

public class CInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        throw new NotImplementedException();
    }
}

public class DInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        throw new NotImplementedException();
    }
}

class CommandA : ICommand
{
}

class CommandB : ICommand
{
}

class CommandC : ICommand
{
}

[Routing]
partial class SimpleSyncPresenter
{
    public void On(CommandA cmd)
    {
    }
}

[Routing]
partial class SimpleAsyncPresenter
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        return default;
    }
}

[Routing]
partial class SimpleCombinedPresenter
{
    public void On(CharacterEnterCommand cmd)
    {
    }

    public UniTask On(CharacterMoveCommand cmd)
    {
        return default;
    }
}
