using NUnit.Framework;
using VitalRouter.Internal;

namespace VitalRouter.Tests;

[TestFixture]
public class ExpandBufferTest
{
    [Test]
    public void RemoveAt()
    {
        var x = new ExpandBuffer<int>(4);
        x.Add(111);
        x.Add(222);
        x.Add(333);
        x.Add(444);

        x.RemoveAt(1);
        Assert.That(x[0], Is.EqualTo(111));
        Assert.That(x[1], Is.EqualTo(333));
        Assert.That(x[2], Is.EqualTo(444));

        x.RemoveAt(2);
        Assert.That(x[0], Is.EqualTo(111));
        Assert.That(x[1], Is.EqualTo(333));
    }
}