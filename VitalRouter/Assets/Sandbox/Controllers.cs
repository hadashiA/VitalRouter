using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VitalRouter;

public readonly struct CharacterMoveCommand : ICommand
{
}

public readonly struct CharacterEnterCommand : ICommand
{
}

public readonly struct CharacterExitCommand : ICommand
{
}

public class HogeInterceptor : ICommandInterceptor
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
[RoutesBefore(typeof(HogeInterceptor))]
[RoutesBefore(typeof(HogeInterceptor))]
public partial class FooPresenter
{
    public async Awaitable On(CharacterMoveCommand cmd)
    {
    }

    [RoutesBefore(typeof(HogeInterceptor))]
    public void On(CharacterExitCommand cmd)
    {
    }

    public void On(CharacterEnterCommand cmd)
    {
    }
}

public partial class FooPresenter : ICommandSubscriber
{
    public void Execute<T>(T command) where T : ICommand
    {
        switch (command)
        {
            case CharacterEnterCommand cmd:
                On(cmd);
                break;
            case CharacterExitCommand cmd:
                On(cmd);
                break;
        }
    }
}