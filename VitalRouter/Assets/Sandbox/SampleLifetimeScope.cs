using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
using VitalRouter;
using VitalRouter.VContainer;

public class SampleEntryPoint : IAsyncStartable
{
    readonly CommandBus commandBus;

    public SampleEntryPoint(CommandBus commandBus)
    {
        this.commandBus = commandBus;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        await commandBus.PublishAsync(new CharacterMoveCommand(), cancellation);
    }
}

public class SampleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<SampleEntryPoint>();

        builder.RegisterVitalRouter(router =>
        {
            router.Map<SamplePresenter>();
            router.Map<RoutingBehaviour>();
        });
    }
}

