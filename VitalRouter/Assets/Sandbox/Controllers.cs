using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Scripting;

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

    public partial class SamplePresenter
    {
        [Preserve]
        public void MapRoutes(ICommandSubscribable subscribable)
        {
            MapRoutes(subscribable, new AInterceptor(), new BInterceptor());
        }

        [Preserve]
        public void MapRoutes(ICommandSubscribable subscribable, AInterceptor aInterceptor, BInterceptor bInterceptor)
        {
            subscribable.Subscribe(new __Subscriber__(this));
            subscribable.Subscribe(new __AsyncSubscriber__(this));
            subscribable.Subscribe(new __InterceptSubscriber__(this, aInterceptor, bInterceptor));
        }

        class __Subscriber__ : ICommandSubscriber
        {
            readonly SamplePresenter source;

            public __Subscriber__(SamplePresenter source)
            {
                this.source = source;
            }

            public void Receive<T>(T command) where T : ICommand
            {
                switch (command)
                {
                    case CharacterExitCommand x:
                        source.On(x);
                        break;
                }
            }
        }

        class __AsyncSubscriber__ : IAsyncCommandSubscriber
        {
            readonly SamplePresenter source;

            public __AsyncSubscriber__(SamplePresenter source)
            {
                this.source = source;
            }

            public UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
            {
                switch (command)
                {
                    case CharacterMoveCommand x:
                        return source.On(x);
                    default:
                        return UniTask.CompletedTask;
                }
            }
        }

        class __InterceptSubscriber__ : IAsyncCommandSubscriber
        {
            readonly __AsyncSubscriber__ core;

            readonly ICommandInterceptor[] interceptorStackDefault;
            readonly ICommandInterceptor[] interceptorStackCharacterMoveCommand;

            public __InterceptSubscriber__(SamplePresenter source, AInterceptor aInterceptor, BInterceptor bInterceptor)
            {
                core = new __AsyncSubscriber__(source);
                interceptorStackDefault = new ICommandInterceptor[]
                {
                    aInterceptor,
                };
                interceptorStackCharacterMoveCommand = new ICommandInterceptor[]
                {
                    aInterceptor,
                    bInterceptor
                };
            }

            public UniTask ReceiveAsync<T>(T command, CancellationToken cancellation = default) where T : ICommand
            {
                switch (command)
                {
                    case CharacterEnterCommand x:
                    {
                        var context = InvokeContext<T>.Rent(interceptorStackDefault, core);
                        try
                        {
                            return context.InvokeRecursiveAsync(command, cancellation);
                        }
                        finally
                        {
                            context.Return();
                        }
                    }
                    default:
                        return default;
                }
            }
        }
    }