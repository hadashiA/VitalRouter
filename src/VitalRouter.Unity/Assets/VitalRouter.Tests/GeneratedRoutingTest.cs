using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

[TestFixture]
public class GeneratedRoutingTest
{
    readonly Router router = new();

    [Test]
    public async Task SimpleSyncRoutes()
    {
        var x = new SimpleSyncPresenter();
        x.MapTo(router);

        await router.PublishAsync(new CommandA(111));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
    }

    [Test]
    public async Task SimpleAsyncRoutes()
    {
        var x = new SimpleAsyncPresenter();
        x.MapTo(router);

        await router.PublishAsync(new CommandA(111));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
    }

    [Test]
    public async Task SimpleAsyncWithCancellationTokenRoutes()
    {
        var x = new SimpleAsyncWithCancellationTokenPresenter();
        x.MapTo(router);

        using var cts = new CancellationTokenSource();

        await router.PublishAsync(new CommandA(111), cts.Token);

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.AreEqual(x.CancellationToken, cts.Token);
    }

    [Test]
    public async Task SimpleCombinedRoutes()
    {
        var x = new SimpleCombinedPresenter();
        x.MapTo(router);

        await router.PublishAsync(new CommandA(111));
        await router.PublishAsync(new CommandB(222));
        await router.PublishAsync(new CommandC(222));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.Count, Is.Zero);
    }

    [Test]
    public async Task DefaultInterceptor()
    {
        var x = new DefaultInterceptorPresenter();
        var interceptorA = new AInterceptor();
        x.MapTo(router, interceptorA);

        await router.PublishAsync(new CommandA(111));
        await router.PublishAsync(new CommandB(222));

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
        var interceptorB = new BInterceptor();
        x.MapTo(router, interceptorA, interceptorB);

        await router.PublishAsync(new CommandA(1));
        await router.PublishAsync(new CommandB(2));
        await router.PublishAsync(new CommandC(3));
        await router.PublishAsync(new CommandD(4));

        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorA.Receives.Count, Is.Zero);

        Assert.That(interceptorB.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorB.Receives.Count, Is.Zero);

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandC>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandD>());
        Assert.That(x.Receives.Count, Is.Zero);
    }

    [Test]
    public async Task DefaultAndPerMethodInterceptors()
    {
        var x = new ComplexInterceptorPresenter();
        var interceptorA = new AInterceptor();
        var interceptorB = new BInterceptor();
        var interceptorC = new CInterceptor();
        var interceptorD = new DInterceptor();
        x.MapTo(router, interceptorA, interceptorB, interceptorC, interceptorD);

        await router.PublishAsync(new CommandA(1));
        await router.PublishAsync(new CommandB(2));
        await router.PublishAsync(new CommandC(3));
        await router.PublishAsync(new CommandD(4));

        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorA.Receives.Dequeue(), Is.InstanceOf<CommandC>());
        Assert.That(interceptorA.Receives.Count, Is.Zero);

        Assert.That(interceptorB.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorB.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorB.Receives.Dequeue(), Is.InstanceOf<CommandC>());
        Assert.That(interceptorB.Receives.Count, Is.Zero);

        Assert.That(interceptorC.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorC.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorC.Receives.Count, Is.Zero);

        Assert.That(interceptorD.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(interceptorD.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(interceptorD.Receives.Count, Is.Zero);

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandB>());
        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandC>());
        Assert.That(x.Receives.Count, Is.Zero);
    }

    [Test]
    public async Task ErrorHandler()
    {
        var x = new ErrorHandlingInterceptorPresenter();
        var errorHandler = new ErrorHandlingInterceptor();
        x.MapTo(router, errorHandler);

        await router.PublishAsync(new CommandA(1));

        Assert.That(errorHandler.Exception, Is.InstanceOf<TestException>());
    }

    [Test]
    public async Task ErrorFromInterceptorHandler()
    {
        var x = new ErrorHandlingInterceptorPresenter2();
        var errorHandler = new ErrorHandlingInterceptor();
        var throwInterceptor = new ThrowInterceptor();
        x.MapTo(router, errorHandler, throwInterceptor);

        await router.PublishAsync(new CommandA(1));

        Assert.That(errorHandler.Exception, Is.InstanceOf<TestException>());
    }

    [Test]
    public async Task MethodAttribute()
    {
        var x = new MethodAttributePresenter();
        x.MapTo(router);

        await router.PublishAsync(new CommandA(1));

        Assert.That(x.Receives.Dequeue(), Is.InstanceOf<CommandA>());
    }
}

[Routes]
partial class SimpleSyncPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    public void On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
    }
}

[Routes]
partial class SimpleAsyncPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }
}

[Routes]
partial class MethodAttributePresenter
{
    public Queue<ICommand> Receives { get; } = new();

    [Route]
    void On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
    }
}

[Routes]
partial class SimpleAsyncWithCancellationTokenPresenter
{
    public Queue<ICommand> Receives { get; } = new();
    public CancellationToken CancellationToken { get; private set; }

    public UniTask On(CommandA cmd, CancellationToken cancellation)
    {
        Receives.Enqueue(cmd);
        CancellationToken = cancellation;
        return default;
    }
}

[Routes]
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

[Routes]
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

[Routes]
partial class PerMethodInterceptorPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    [Filter(typeof(AInterceptor))]
    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    [Filter(typeof(BInterceptor))]
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

[Routes]
[Filter(typeof(AInterceptor))]
#if NET7_0_OR_GREATER
[Filter<BInterceptor>]
#else
[Filter(typeof(BInterceptor))]
#endif
partial class ComplexInterceptorPresenter
{
    public Queue<ICommand> Receives { get; } = new();

    [Filter(typeof(CInterceptor))]
    [Filter(typeof(DInterceptor))]
    public UniTask On(CommandA cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    [Filter(typeof(CInterceptor))]
#if NET7_0_OR_GREATER
    [Filter<DInterceptor>]
#else
    [Filter(typeof(DInterceptor))]
#endif
    public void On(CommandB cmd)
    {
        Receives.Enqueue(cmd);
    }

    public UniTask On(CommandC cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }
}

[Routes]
[Filter(typeof(ErrorHandlingInterceptor))]
partial class ErrorHandlingInterceptorPresenter
{
    public UniTask On(CommandA cmd)
    {
        throw new TestException();
    }
}

[Routes]
[Filter(typeof(ErrorHandlingInterceptor))]
[Filter(typeof(ThrowInterceptor))]
partial class ErrorHandlingInterceptorPresenter2
{
    public UniTask On(CommandA cmd)
    {
        return default;
    }
}

[Routes]
partial class TaskPresenter
{
    Func<DefaultInterceptorPresenter, int, PublishContext, UniTask>? Value;
    public Queue<ICommand> Receives { get; } = new();

    public Task On(CommandA cmd)
    {
        Value = static async (source, command, context) => await V();
        Receives.Enqueue(cmd);
        return Task.CompletedTask;
    }

    public ValueTask On(CommandB cmd)
    {
        Receives.Enqueue(cmd);
        return default;
    }

    public static ValueTask V()
    {
        return default;
    }
}
