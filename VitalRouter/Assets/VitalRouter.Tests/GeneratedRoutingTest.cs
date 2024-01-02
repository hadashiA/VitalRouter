using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

[TestFixture]
public class GeneratedRoutingTest
{
    readonly CommandBus commandBus = new();

    [Test]
    public async Task SimpleSyncRoutes()
    {
        var x = new SimpleSyncPresenter();
        x.MapRoutes(commandBus);

        await commandBus.PublishAsync(new CommandA(111));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
    }

    [Test]
    public async Task SimpleAsyncRoutes()
    {
        var x = new SimpleAsyncPresenter();
        x.MapRoutes(commandBus);

        await commandBus.PublishAsync(new CommandA(111));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
    }

    [Test]
    public async Task SimpleCombinedRoutes()
    {
        var x = new SimpleCombinedPresenter();
        x.MapRoutes(commandBus);

        await commandBus.PublishAsync(new CommandA(111));
        await commandBus.PublishAsync(new CommandB(222));
        await commandBus.PublishAsync(new CommandC(222));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.TryDequeue(out _), Is.False);
    }
}

[Routing]
partial class SimpleSyncPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    public void On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
    }
}

[Routing]
partial class SimpleAsyncPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }
}

[Routing]
partial class SimpleCombinedPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    public void On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
    }

    public UniTask On(CommandB cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }
}

[Routing]
[Filter(typeof(AInterceptor))]
partial class DefaultInterceptorPresenter
{
    public UniTask On(CommandA cmd)
    {
        return default;
    }

    public void On(CommandB cmd)
    {
    }
}
