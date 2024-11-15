using System.Threading;
using Cysharp.Threading.Tasks;
using MyNamespace;
using Sandbox;
using UnityEngine.Profiling;
using VContainer;
using VContainer.Unity;
using VitalRouter;
using VitalRouter.VContainer;

public class SampleEntryPoint : IAsyncStartable
{
    readonly Router router;

    public SampleEntryPoint(Router router)
    {
        this.router = router;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        Profiler.BeginSample("Publish!");
        _ = router.PublishAsync(new CharacterEnterCommand(), cancellation);
        Profiler.EndSample();
    }
}

public class SampleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterVitalRouter(routing =>
        {
            routing.Filters.Add<LoggingInterceptor>();
            routing.MapEntryPoint<SamplePresenter>();
            routing.MapComponentOnNewGameObject<RoutingBehaviour>();

            // routing.FanOut(childRouter =>
            // {
            //     childRouter.Map<SamplePresenter2>();
            // });

            // routing.FanOut(childRouter =>
            // {
            //     childRouter.Map<SamplePresenter3>();
            //
            //     childRouter.FanOut(grandChildRouter =>
            //     {
            //         grandChildRouter.Map<SamplePresenter4>();
            //     });
            //
            //     childRouter.FanOut(grandChildRouter =>
            //     {
            //         grandChildRouter.Map<SamplePresenter5>();
            //     });
            // });
        });
    }
}

