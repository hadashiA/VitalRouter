using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

class TestInterceptor : IAsyncCommandInterceptor
{
    public int Calls { get; private set; }

    public UniTask InvokeAsync<T>(T command, CancellationToken cancellation, Func<T, CancellationToken, UniTask> next)
        where T : ICommand
    {
        Calls++;
        return UniTask.CompletedTask;
    }
}

struct TestCommand1 : ICommand
{
    public int X;
}

struct TestCommand2 : ICommand
{
    public int X;
}

[TestFixture]
public class CommandBusTest
{
    [Test]
    public void NoInterceptors()
    {
        var commandBus = new CommandBus();

        var subscriber1Calls = 0;
        var subscriber2Calls = 0;
        var subscriber3Calls = 0;

        commandBus.Subscribe<TestCommand1>(cmd => subscriber1Calls++);
    }

    [Test]
    public async Task PropagateInterceptors()
    {
        var commandBus = new CommandBus();
        var interceptor1 = new TestInterceptor();
        var interceptor2 = new TestInterceptor();
        commandBus.Use(interceptor1);
        commandBus.Use(interceptor2);

        await commandBus.PublishAsync(new TestCommand1());

        Assert.That(interceptor1.Calls, Is.EqualTo(1));
        Assert.That(interceptor2.Calls, Is.EqualTo(1));
    }

    [Test]
    public void StopPropagationByInterceptor()
    {
    }
}
