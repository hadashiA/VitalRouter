using System.Threading.Tasks;
using NUnit.Framework;

namespace VitalRouter.Tests;

class PoolableCommand : IPoolableCommand
{
    public void OnReturnToPool()
    {
    }
}

[TestFixture]
public class CommandPoolingTest
{
    [Test]
    public async Task ReturnToCommandPool()
    {
        var commandPooling = new CommandPooling();
        var router = new Router().Filter(commandPooling);

        await router.PublishAsync(new PoolableCommand());

        var pool = (ConcurrentQueueCommandPool<PoolableCommand>)CommandPool<PoolableCommand>.Shared;

        Assert.That(pool.queue.TryDequeue(out var command), Is.True);
    }
}