using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using VitalRouter.Internal;

namespace VitalRouter.Tests;

[TestFixture]
class UniTaskAsyncLockTest
{
    [Test]
    public async Task WaitAsync_One()
    {
        var x = new UniTaskAsyncLock();
        await x.WaitAsync();
        x.Release();
    }

    [Test]
    public async Task WaitAsync_Multiple()
    {
        var x = new UniTaskAsyncLock();

        var exec1 = false;
        var exec2 = false;

        var run1 = Task.Run(async () =>
        {
            await x.WaitAsync();
            x.Release();
            exec1 = true;
        });

        var run2 = Task.Run(async () =>
        {
            await x.WaitAsync();
            x.Release();
            exec2 = true;
        });

        await Task.WhenAll(run1, run2);

        Assert.That(exec1, Is.True);
        Assert.That(exec2, Is.True);
    }

    [Test]
    public void Wait_One()
    {
        var x = new UniTaskAsyncLock();
        x.Wait();
        x.Release();
    }

    [Test]
    public void Wait_Multiple()
    {
        var x = new UniTaskAsyncLock();

        var result1 = false;
        var result2 = false;

        var t1 = new Thread(() =>
        {
            x.Wait();
            x.Release();
            result1 = true;
        });
        var t2 = new Thread(() =>
        {
            x.Wait();
            x.Release();
            result2 = true;
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public async Task WaitSyncWithAsync()
    {
        var x = new UniTaskAsyncLock();

        var result1 = false;
        var result2 = false;

        var t1 = x.WaitAsync().ContinueWith(async () =>
        {
            await Task.Delay(100);
            x.Release();
            Volatile.Write(ref result1, true);
        });

        var t2 = new Thread(() =>
        {
            x.Wait();
            x.Release();
            Volatile.Write(ref result2, true);
        });

        t2.Start();
        await t1;
        t2.Join();

        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }
}