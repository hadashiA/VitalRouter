using Cysharp.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VitalRouter.Extensions.DependencyInjection.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTest
{
    [Test]
    public async Task MapAll()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<DependencyA>();
        builder.Services.AddSingleton<DependencyB>();

        builder.Services.AddVitalRouter(routing =>
        {
            routing.Filters.Add<TestInterceptor3>();

            routing.MapAll(GetType().Assembly);
        });

        var app = builder.Build();
        await app.StartAsync();

        var router = app.Services.GetRequiredService<Router>();
        var p1 = app.Services.GetRequiredService<TestPresenter1>();
        var p2 = app.Services.GetRequiredService<TestPresenter2>();
        var i1 = app.Services.GetRequiredService<TestInterceptor1>();
        var i2 = app.Services.GetRequiredService<TestInterceptor2>();

        await router.PublishAsync(new FooCommand());

        Assert.That(p1.Calls, Is.EqualTo(1));
        Assert.That(p2.Calls, Is.EqualTo(1));
        Assert.That(i1.Calls, Is.EqualTo(1));
        Assert.That(i2.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task Map()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<DependencyA>();
        builder.Services.AddSingleton<DependencyB>();

        builder.Services.AddVitalRouter(routing =>
        {
            routing.Map<TestPresenter1>();
            routing.Map<TestPresenter2>();
        });

        var app = builder.Build();
        await app.StartAsync();

        var router = app.Services.GetRequiredService<Router>();
        var p1 = app.Services.GetRequiredService<TestPresenter1>();
        var p2 = app.Services.GetRequiredService<TestPresenter2>();
        var i1 = app.Services.GetRequiredService<TestInterceptor1>();
        var i2 = app.Services.GetRequiredService<TestInterceptor2>();

        await router.PublishAsync(new FooCommand());

        Assert.That(p1.Calls, Is.EqualTo(1));
        Assert.That(p2.Calls, Is.EqualTo(1));
        Assert.That(i1.Calls, Is.EqualTo(1));
        Assert.That(i2.Calls, Is.EqualTo(1));
    }


    [Test]
    public async Task FanOut()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<DependencyA>();
        builder.Services.AddSingleton<DependencyB>();

        builder.Services.AddVitalRouter(routing =>
        {
            routing.FanOut(nested =>
            {
                nested.Map<TestPresenter1>();
            });
            routing.FanOut(nested =>
            {
                nested.Filters.Add<TestInterceptor3>();
                nested.Map<TestPresenter2>();
            });
        });

        var app = builder.Build();
        await app.StartAsync();

        var router = app.Services.GetRequiredService<Router>();
        var p1 = app.Services.GetRequiredService<TestPresenter1>();
        var p2 = app.Services.GetRequiredService<TestPresenter2>();
        var i1 = app.Services.GetRequiredService<TestInterceptor1>();
        var i2 = app.Services.GetRequiredService<TestInterceptor2>();
        var i3 = app.Services.GetRequiredService<TestInterceptor3>();

        await router.PublishAsync(new FooCommand());

        Assert.That(p1.Calls, Is.EqualTo(1));
    }
}

readonly struct FooCommand : ICommand;

class DependencyA;
class DependencyB;

class TestInterceptor1 : ICommandInterceptor
{
    public int Calls { get; private set; }

    public UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        Calls++;
        return next(command, context);
    }
}

class TestInterceptor2(DependencyA dependency) : ICommandInterceptor
{
    public int Calls { get; private set; }

    public UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        Calls++;
        return next(command, context);
    }
}

class TestInterceptor3(DependencyB dependency) : ICommandInterceptor
{
    public int Calls { get; private set; }

    public UniTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next) where T : ICommand
    {
        Calls++;
        return next(command, context);
    }
}

[Routes]
partial class TestPresenter1
{
    public int Calls { get; private set; }

    public void On(FooCommand cmd)
    {
        Calls++;
    }
}

[Routes]
[Filter(typeof(TestInterceptor1))]
partial class TestPresenter2(DependencyB dependencyB)
{
    public int Calls { get; private set; }

    [Filter(typeof(TestInterceptor2))]
    public void On(FooCommand cmd)
    {
        Calls++;
    }
}
