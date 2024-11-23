using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Sandbox;
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
        await router.PublishAsync(new CharacterEnterCommand(), cancellation);
    }
}

public class TestInterceptor : ICommandInterceptor
{
    public string Name { get; set; }

    public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        try
        {
            await next(command, context);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log($"ERROR DANE! {e.Message}");
        }
    }
}

public class SampleLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(Router.Default);
        Router.Default.AddFilter(new TestInterceptor
        {
            Name = "Default!!"
        });

        builder.RegisterEntryPoint<SampleEntryPoint>();

        builder.RegisterVitalRouter(routing =>
        {
            routing.Filters.Add<LoggingInterceptor>();
            routing.MapEntryPoint<SamplePresenter>();
            // routing.MapComponent(b)<RoutingBehaviour>();

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

