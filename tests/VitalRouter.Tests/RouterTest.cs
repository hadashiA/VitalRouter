using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

class TestSubscriber : ICommandSubscriber
{
    public int Calls { get; private set; }

    public void Receive<T>(T command, PublishContext context) where T : ICommand
    {
        Calls++;
    }
}

class TestAsyncSubscriber : IAsyncCommandSubscriber
{
    public int Calls { get; private set; }
    public ICommand? LastCommand { get; private set; }

    public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), context.CancellationToken);
        Calls++;
        LastCommand = command;
    }
}

class TestSignalSubscriber : IAsyncCommandSubscriber, IDisposable
{
    public AutoResetEvent Signal { get; } = new(false);
    public int Calls { get; private set; }
    public int Completed { get; private set; }
    public ICommand? LastCommand { get; private set; }

    public async ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
    {
        Calls++;

        if (context.CancellationToken.IsCancellationRequested)
        {
            return;
        }

        await Task.Run(() =>
        {
            Signal.WaitOne();

            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            Completed++;
            LastCommand = command;
        });
    }

    public void Dispose()
    {
        Signal.Dispose();
    }
}

class TestInterceptor : ICommandInterceptor
{
    public int Calls { get; private set; }

    public ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next)
        where T : ICommand
    {
        Calls++;
        return next(command, ctx);
    }
}

class TestStopperInterceptor : ICommandInterceptor
{
    public ValueTask InvokeAsync<T>(T command, PublishContext ctx, PublishContinuation<T> next) where T : ICommand
    {
        return default;
    }
}

class TestThrowSubscriber : ICommandSubscriber
{
    public void Receive<T>(T command, PublishContext ctx) where T : ICommand
    {
        throw new TestException();
    }
}

struct TestCommand1 : ICommand
{
    public string Id;
}

struct TestCommand2 : ICommand
{
    public string Id;
}

[TestFixture]
public class RouterTest
{
    [Test]
    public async Task Subscribers()
    {
        var commandBus = new Router();

        var subscriber1 = new TestSubscriber();
        var subscriber2 = new TestSubscriber();
        var subscriber3 = new TestAsyncSubscriber();
        var subscriber4 = new TestAsyncSubscriber();

        commandBus.Subscribe(subscriber1);
        commandBus.Subscribe(subscriber2);
        commandBus.Subscribe(subscriber3);
        commandBus.Subscribe(subscriber4);

        await commandBus.PublishAsync(new TestCommand1());
        Assert.That(subscriber1.Calls, Is.EqualTo(1));
        Assert.That(subscriber2.Calls, Is.EqualTo(1));
        Assert.That(subscriber3.Calls, Is.EqualTo(1));
        Assert.That(subscriber4.Calls, Is.EqualTo(1));

        await commandBus.PublishAsync(new TestCommand1());
        Assert.That(subscriber1.Calls, Is.EqualTo(2));
        Assert.That(subscriber2.Calls, Is.EqualTo(2));
        Assert.That(subscriber3.Calls, Is.EqualTo(2));
        Assert.That(subscriber4.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task PropagateInterceptors()
    {
        var commandBus = new Router();
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestInterceptor();
        var router = commandBus
            .WithFilter(interceptor1)
            .WithFilter(interceptor2);

        await router.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(interceptor2.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task StopPropagationByInterceptor()
    {
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestStopperInterceptor();
        var subscriber1 = new TestSubscriber();
        var router = new Router()
            .WithFilter(interceptor1)
            .WithFilter(interceptor2);
        router.Subscribe(subscriber1);

        await router.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(subscriber1.Calls, Is.Zero);
    }

    [Test]
    public async Task ErrorHandlingInterceptor()
    {
        var commandBus = new Router();
        var errorHandler = new ErrorHandlingInterceptor();
        var router = commandBus
            .WithFilter(errorHandler);

        router.Subscribe(new TestThrowSubscriber());
        await router.PublishAsync(new TestCommand1());

        Assert.That(errorHandler.Exception, Is.InstanceOf<TestException>());
    }

    // Regression for https://github.com/hadashiA/VitalRouter/issues/138
    [Test]
    public async Task WithFilterSubscribeReceivesPublishFromParent()
    {
        var router = new Router();
        var interceptor = new TestInterceptor();
        var subscriber = new TestSubscriber();

        router.WithFilter(interceptor).Subscribe(subscriber);

        await router.PublishAsync(new TestCommand1());

        Assert.That(interceptor.Calls, Is.EqualTo(1));
        Assert.That(subscriber.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task WithFilterChainedSubscribeReceivesPublishFromParent()
    {
        var root = new Router();
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestInterceptor();
        var subscriber = new TestSubscriber();

        root.WithFilter(interceptor1)
            .WithFilter(interceptor2)
            .Subscribe(subscriber);

        await root.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(interceptor2.Calls, Is.EqualTo(1));
        Assert.That(subscriber.Calls, Is.EqualTo(1));
    }

    // Subscribers at multiple depths must not cause ancestor filters to run multiple times.
    [Test]
    public async Task WithFilterDoesNotDoubleExecuteAncestorFilters()
    {
        var root = new Router();
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestInterceptor();
        var subscriberOnV1 = new TestSubscriber();
        var subscriberOnV2 = new TestSubscriber();

        var v1 = root.WithFilter(interceptor1);
        var v2 = v1.WithFilter(interceptor2);
        v1.Subscribe(subscriberOnV1);
        v2.Subscribe(subscriberOnV2);

        await root.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(interceptor2.Calls, Is.EqualTo(1));
        Assert.That(subscriberOnV1.Calls, Is.EqualTo(1));
        Assert.That(subscriberOnV2.Calls, Is.EqualTo(1));
    }

    // Publishing directly on the chain end runs the cumulative chain (Rx Where-like).
    [Test]
    public async Task WithFilterChainedDirectPublishRunsCumulativeChain()
    {
        var root = new Router();
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestInterceptor();
        var subscriber = new TestSubscriber();

        var v2 = root.WithFilter(interceptor1).WithFilter(interceptor2);
        v2.Subscribe(subscriber);

        await v2.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(interceptor2.Calls, Is.EqualTo(1));
        Assert.That(subscriber.Calls, Is.EqualTo(1));
    }

    [Test]
    public async Task WithFilterDisposeRemovesChildFromParent()
    {
        var root = new Router();
        var interceptor = new TestInterceptor();
        var subscriber = new TestSubscriber();

        var view = root.WithFilter(interceptor);
        view.Subscribe(subscriber);

        view.Dispose();

        await root.PublishAsync(new TestCommand1());

        Assert.That(interceptor.Calls, Is.Zero);
        Assert.That(subscriber.Calls, Is.Zero);
    }

    [Test]
    public void ConcurrentPublishing()
    {
        var commandBus = new Router();
        using var subscriber1 = new TestSignalSubscriber();
        commandBus.Subscribe(subscriber1);

        _ = commandBus.PublishAsync(new TestCommand1());
        _ = commandBus.PublishAsync(new TestCommand2());

        Assert.That(subscriber1.Calls, Is.EqualTo(2));
    }

    [Test]
    public async Task SequentialOrdering()
    {
        var commandBus = new Router();
        commandBus.AddFilter(CommandOrdering.Sequential);

        using var subscriber1 = new TestSignalSubscriber();
        commandBus.Subscribe(subscriber1);

        var task1 = commandBus.PublishAsync(new TestCommand1());
        var task2 = commandBus.PublishAsync(new TestCommand2());

        Assert.That(subscriber1.Calls, Is.EqualTo(1));
        Assert.That(subscriber1.Completed, Is.EqualTo(0));

        subscriber1.Signal.Set();
        await task1;

        Assert.That(subscriber1.Completed, Is.EqualTo(1));
        // Assert.That(subscriber1.Calls, Is.EqualTo(2));

        subscriber1.Signal.Set();
        await task2;
        Assert.That(subscriber1.Completed, Is.EqualTo(2));
    }

    [Test]
    public async Task SwitchOrdering()
    {
        var commandBus = new Router();
        commandBus.AddFilter(CommandOrdering.Switch);

        var subscriber1 = new TestAsyncSubscriber();
        commandBus.Subscribe(subscriber1);

        var cmd1 = new TestCommand1 { Id = "1" };
        var cmd2 = new TestCommand1 { Id = "2" };
        var t1 = commandBus.PublishAsync(cmd1);
        var t2 = commandBus.PublishAsync(cmd2);

        try { await t1; }
        catch (TaskCanceledException) { }
        catch (AggregateException) { }
        await t2;

        Assert.That(subscriber1.Calls, Is.EqualTo(1));
        Assert.That(((TestCommand1)subscriber1.LastCommand!).Id, Is.EqualTo("2"));
    }

    [Test]
    public async Task DropOrdering()
    {
        var commandBus = new Router();
        commandBus.AddFilter(CommandOrdering.Drop);

        var subscriber1 = new TestAsyncSubscriber();
        commandBus.Subscribe(subscriber1);

        var cmd1 = new TestCommand1 { Id = "1" };
        var cmd2 = new TestCommand1 { Id = "2" };
        var t1 = commandBus.PublishAsync(cmd1);
        var t2 = commandBus.PublishAsync(cmd2);

        await t1;
        await t2;

        Assert.That(subscriber1.Calls, Is.EqualTo(1));
        Assert.That(((TestCommand1)subscriber1.LastCommand!).Id, Is.EqualTo("1"));
    }
}