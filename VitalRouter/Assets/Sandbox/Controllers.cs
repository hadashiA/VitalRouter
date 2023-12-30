using System;
using System.Threading;
using Cysharp.Threading.Tasks;

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



[Routing]
[Filter(typeof(HogeInterceptor))]
[Filter(typeof(HogeInterceptor))]
public partial class FooPresenter
{
    public async UniTask On(CharacterMoveCommand cmd)
    {
    }

    [Filter(typeof(HogeInterceptor))]
    public void On(CharacterExitCommand cmd)
    {
    }

    public void On(CharacterEnterCommand cmd)
    {
    }
}
//
// public partial class FooPresenter : ICommandSubscriber
// {
//     public void Receive<T>(T command) where T : ICommand
//     {
//         switch (command)
//         {
//             case CharacterEnterCommand cmd:
//                 On(cmd);
//                 break;
//             case CharacterExitCommand cmd:
//                 On(cmd);
//                 break;
//         }
//     }
// }
