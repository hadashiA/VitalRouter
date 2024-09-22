#if VITALROUTER_UNITASK_INTEGRAION
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using VitalRouter.Internal;

namespace VitalRouter.Tests
{
[TestFixture]
public class WhenAllTest
{
    [Test]
    public async Task WaitAll()
    {
        var resutl1 = false;
        var result2 = false;
        var task1 = UniTask.RunOnThreadPool(() =>
        {
            resutl1 = true;
        });
        var task2 = UniTask.RunOnThreadPool(() =>
        {
            result2 = true;
        });

        await WhenAllUtility.WhenAll(task1, task2);

        Assert.That(resutl1, Is.True);
        Assert.That(result2, Is.True);
    }
}
}
#endif