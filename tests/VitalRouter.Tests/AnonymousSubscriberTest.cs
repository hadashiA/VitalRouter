using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests
{
struct  TestCommand1 : ICommand
{
    public string Id { get; set; }
}

public class AnonymousSubscriberTest
{
    [Test]
    public async Task SubscribeAwait_Sequential()
    {
        var commandBus = new Router();
        ICommand? executings = null;
        var q = new Queue<TestCommand1>();
        commandBus.SubscribeAwait<TestCommand1>(async (cmd, ctx) =>
        {
            if (executings != null)
            {
                throw new InvalidOperationException();
            }

            executings = cmd;
            try
            {
                await Task.Delay(500, ctx.CancellationToken);
            }
            finally
            {
                executings = null;
            }
            q.Enqueue(cmd);
        }, CommandOrdering.Sequential);

        var t1 = commandBus.PublishAsync(new TestCommand1 { Id = "1" });
        var t2 = commandBus.PublishAsync(new TestCommand1 { Id = "2" });
        await t2;

        Assert.That(q, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task SubscribeAwait_Drop()
    {
        var commandBus = new Router();
        var q = new Queue<TestCommand1>();
        commandBus.SubscribeAwait<TestCommand1>(async (cmd, ctx) =>
        {
            await Task.Delay(500, ctx.CancellationToken);
            q.Enqueue(cmd);
        }, CommandOrdering.Drop);

        var t1 = commandBus.PublishAsync(new TestCommand1 { Id = "1" });
        var t2 = commandBus.PublishAsync(new TestCommand1 { Id = "2" });
        await t1;
        await t2;

        Assert.That(q, Has.Count.EqualTo(1));
        Assert.That(q.Dequeue().Id, Is.EqualTo("1"));
    }

    [Test]
    public async Task SubscribeAwait_Switch()
    {
        var commandBus = new Router();
        var q = new Queue<TestCommand1>();
        commandBus.SubscribeAwait<TestCommand1>(async (cmd, ctx) =>
        {
            await Task.Delay(500, ctx.CancellationToken);
            q.Enqueue(cmd);
        }, CommandOrdering.Switch);

        var t1 = commandBus.PublishAsync(new TestCommand1 { Id = "1" });
        var t2 = commandBus.PublishAsync(new TestCommand1 { Id = "2" });

        try { await t1; }
        catch (TaskCanceledException) { }
        catch (AggregateException) { }
        await t2;

        Assert.That(q, Has.Count.EqualTo(1));
        Assert.That(q.Dequeue().Id, Is.EqualTo("2"));
    }
}
}
