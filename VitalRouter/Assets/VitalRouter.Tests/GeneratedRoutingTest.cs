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
        Assert.That(x.Receives.Count, Is.Zero);
    }

    [Test]
    public async Task DefaultInterceptor()
    {
        var x = new DefaultInterceptorPresenter();
        var interceptorA = new AInterceptor();
        x.MapRoutes(commandBus, interceptorA);

        await commandBus.PublishAsync(new CommandA(111));
        await commandBus.PublishAsync(new CommandB(222));

        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorA.Receives.Count, Is.Zero);

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.Count, Is.Zero);
    }

    [Test]
    public async Task PerMethodInterceptors()
    {
        var x = new PerMethodInterceptorPresenter();
        var interceptorA = new AInterceptor();
        x.MapRoutes(commandBus, interceptorA);

        await commandBus.PublishAsync(new CommandA(111));
        await commandBus.PublishAsync(new CommandB(222));

        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorA.Receives.Count, Is.Zero);

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.Count, Is.Zero);
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
    public Queue<ICommand> Receives { get; } = new();

    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    public void On(CommandB cmd)
    {
        Receives.Enqueue(cmd);
    }
}

[Routing]
partial class PerMethodInterceptorPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    [Filter(typeof(AInterceptor))]
    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    [Filter(typeof(AInterceptor))]
    public void On(CommandB cmd)
    {
        Receives.Enqueue(cmd);
    }

    public UniTask On(CommandC cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    public void On(CommandD cmd)
    {
        Receives.Enqueue(cmd);
    }
}

