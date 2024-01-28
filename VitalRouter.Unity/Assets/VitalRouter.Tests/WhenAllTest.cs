#if UNITY_2021_3_OR_NEWER
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using VitalRouter.Internal;

namespace VitalRouter.Tests;

[TestFixture]
public class WhenAllTest
{
    [Test]
    public async Task WaitAll()
    {
        var promise = new ReusableWhenAllSource();

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

        promise.Reset(new[] { task1, task2 });
        await promise.Task;

        Assert.That(resutl1, Is.True);
        Assert.That(result2, Is.True);
    }
}
#endif