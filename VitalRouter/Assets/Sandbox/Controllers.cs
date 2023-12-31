using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter.Internal;

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

// [Routing]
[Filter(typeof(AInterceptor))]
[Filter(typeof(BInterceptor))]
public partial class SyncInterceptorPresenter
{

    public void On(CharacterExitCommand cmd)
    {
    }

    [Filter(typeof(CInterceptor))]
    [Filter(typeof(DInterceptor))]
    public void On(CharacterEnterCommand cmd)
    {
    }
}

public partial class SyncInterceptorPresenter : IAsyncCommandSubscriber
{
    readonly ICommandInterceptor[] __defaultInterceptorStack__ = new ICommandInterceptor[]
    {
        new AInterceptor(),
        new BInterceptor(),
    };

    readonly ICommandInterceptor[] __CharacterEnterCommand_InterceptroStack__ = new ICommandInterceptor[]
    {
        new CInterceptor(),
        new DInterceptor(),
    };

    public UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
    {
        return ReceiveRecursiveAsync(command, cancellation, -1);
    }

    struct ReceiveContext<T> where T : ICommand
    {
    }

    UniTask ReceiveRecursiveAsync<T>(
        T command,
        CancellationToken cancellation,
        int currentInterceptorIndex) where T : ICommand
    {
        ++currentInterceptorIndex;
        if (currentInterceptorIndex <= __defaultInterceptorStack__.Length - 1)
        {
            return __defaultInterceptorStack__[currentInterceptorIndex].InvokeAsync(command, cancellation, ReceiveRecursiveAsync);
        }
        if (currentInterceptorIndex <= __CharacterEnterCommand_InterceptroStack__.Length - 1)
        {
            return __CharacterEnterCommand_InterceptroStack__[currentInterceptorIndex].InvokeAsync(command, cancellation, ReceiveRecursiveAsync);
        }
        if (MoveNextInterceptor(out var interceptor))
        {
            return interceptor.InvokeAsync(command, cancellation, ReceiveRecursiveAsync);
        }
        return commandBus.PublishCoreAsync(command, cancellation);
    }
}



[Routing]
[Filter(typeof(AInterceptor))]
[Filter(typeof(BInterceptor))]
public partial class FooPresenter
{
    public async UniTask On(CharacterMoveCommand cmd)
    {
    }

    [Filter(typeof(AInterceptor))]
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
