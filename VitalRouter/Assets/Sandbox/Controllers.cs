using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter;

public readonly struct CharacterMoveCommand : ICommand
{
}

public readonly struct CharacterEnterCommand : ICommand
{
}

public readonly struct CharacterExitCommand : ICommand
{
}

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

class BInterceptor : ICommandInterceptor
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

[Filter(typeof(AInterceptor))]
public partial class SamplePresenter
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        return default;
    }

    [Filter(typeof(BInterceptor))]
    public UniTask On(CharacterMoveCommand cmd)
    {
        return default;
    }

    public void On(CharacterExitCommand cmd)
    {
    }
}
