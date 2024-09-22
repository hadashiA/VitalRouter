using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Sandbox;
using UnityEngine;
using VitalRouter;

public class LoggingInterceptor : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next) where T : ICommand
    {
        var path = ctx.CallerFilePath.Replace(Application.dataPath, "Assets");
        UnityEngine.Debug.Log($"publish {ctx.CallerMemberName} at (<a href=\"{path}\" line=\"{ctx.CallerLineNumber}\">{path}:{ctx.CallerLineNumber}</a>) {command.GetType()}");
        await next(command, ctx);
    }
}

public class AInterceptor : ICommandInterceptor
{
    public ValueTask InvokeAsync<T>(T command, PublishContext cancellation, PublishContinuation<T> next)
        where T : ICommand
    {
        return next(command, cancellation);
    }
}

public class BInterceptor : ICommandInterceptor
{
    public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
        where T : ICommand
    {
        return next(command, context);
    }
}

[Routes]
// [Filter(typeof(LoggingInterceptor))]
[Filter(typeof(AInterceptor))]
public partial class SamplePresenter
{
    public SamplePresenter()
    {
        UnityEngine.Debug.Log("SamplePresenter.ctor");
    }

    public UniTask On(CharacterEnterCommand cmd)
    {
        UnityEngine.Debug.Log("SamplePresenter.ctor");
        return default;
    }

    public UniTask On(CharacterMoveCommand cmd)
    {
        return default;
    }

    public void On(CharacterExitCommand cmd)
    {
        UnityEngine.Debug.Log($"SamplePresenter.On({cmd.ToString()})");
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
