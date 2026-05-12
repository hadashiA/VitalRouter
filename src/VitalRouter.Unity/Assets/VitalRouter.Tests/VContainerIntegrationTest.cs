#if VITALROUTER_VCONTAINER_INTEGRATION
using System.Threading.Tasks;
using NUnit.Framework;
using VContainer;
using VitalRouter.VContainer;

namespace VitalRouter.Tests
{
[TestFixture]
class VContainerIntegrationTest
{
    public readonly struct PingCommand : ICommand
    {
        public readonly int Value;
        public PingCommand(int value) { Value = value; }
    }

    [Routes]
    public partial class RoutedPresenter
    {
        public int Received { get; private set; }

        public void On(PingCommand cmd)
        {
            Received = cmd.Value;
        }
    }

    [Test]
    public void RegisterVitalRouter_BuildsWithoutCircularDependency()
    {
        // Regression for https://github.com/hadashiA/VitalRouter/issues/140
        var builder = new ContainerBuilder();
        builder.RegisterVitalRouter(routing =>
        {
            routing.Map<RoutedPresenter>();
        });

        Assert.DoesNotThrow(() =>
        {
            using var container = builder.Build();
            var router = container.Resolve<Router>();
            Assert.That(router, Is.Not.Null);
        });
    }

    [Test]
    public async Task RegisterVitalRouter_PublishReachesMappedPresenter()
    {
        var builder = new ContainerBuilder();
        builder.RegisterVitalRouter(routing =>
        {
            routing.Map<RoutedPresenter>();
        });

        using var container = builder.Build();
        var presenter = container.Resolve<RoutedPresenter>();
        var publisher = container.Resolve<ICommandPublisher>();

        await publisher.PublishAsync(new PingCommand(42));

        Assert.That(presenter.Received, Is.EqualTo(42));
    }

    [Test]
    public void RegisterVitalRouter_WithFilter_DoesNotThrow()
    {
        var builder = new ContainerBuilder();
        builder.RegisterVitalRouter(routing =>
        {
            routing.Map<RoutedPresenter>();
        });

        using var container = builder.Build();
        var router = container.Resolve<Router>();

        Assert.DoesNotThrow(() =>
        {
            using var child = router.WithFilter(new NoopInterceptor());
        });
    }

    class NoopInterceptor : ICommandInterceptor
    {
        public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
            where T : ICommand
            => next(command, context);
    }
}
}
#endif