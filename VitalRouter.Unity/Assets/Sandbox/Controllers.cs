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

public class LoggingInterceptor : ICommandInterceptor
{
    public UniTask InvokeAsync<T>(
        T command,
        CancellationToken cancellation,
        Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        UnityEngine.Debug.Log($"{GetType()} {command.GetType()}");
        return next(command, cancellation);
    }
}

public class AInterceptor : ICommandInterceptor
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

[Routes]
[Filter(typeof(LoggingInterceptor))]
public partial class SamplePresenter
{
    public SamplePresenter()
    {
        UnityEngine.Debug.Log("SamplePresenter.ctor");
    }

    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }

    public UniTask On(CharacterMoveCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }

    public void On(CharacterExitCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
    }
}

[Routes]
public partial class SamplePresenter2
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }
}

[Routes]
public partial class SamplePresenter3
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }
}

[Routes]
public partial class SamplePresenter4
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }
}

[Routes]
public partial class SamplePresenter5
{
    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log($"{GetType()} {cmd.GetType()}");
        return default;
    }
}
