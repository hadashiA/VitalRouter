using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

public class SampleChildLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterVitalRouter(routing =>
        {
            routing.Map<SamplePresenter2>();
        });

        builder.RegisterEntryPoint<SampleEntryPoint>();
    }
}
