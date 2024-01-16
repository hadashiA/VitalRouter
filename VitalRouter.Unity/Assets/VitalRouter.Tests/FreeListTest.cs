using NUnit.Framework;
using VitalRouter.Internal;

namespace VitalRouter.Tests;

[TestFixture]
public class FreeListTest
{
    [Test]
    public void Remove()
    {
        var freeList = new FreeList<string>(4);
        freeList.Add("a");
        freeList.Add("b");
        freeList.Add("c");
        freeList.Add("d");

        freeList.Remove("b");

        var span = freeList.AsSpan();
        Assert.That(span.Length, Is.EqualTo(4));
        Assert.That(span[0], Is.EqualTo("a"));
        Assert.That(span[1], Is.Null);
        Assert.That(span[2], Is.EqualTo("c"));
        Assert.That(span[3], Is.EqualTo("d"));
    }

    [Test]
    public void Add_FreeArea()
    {
        var freeList = new FreeList<string>(4);
        freeList.Add("a");
        freeList.Add("b");
        freeList.Add("c");
        freeList.Add("d");

        freeList.Remove("b");
        freeList.Add("e");

        var span = freeList.AsSpan();
        Assert.That(span.Length, Is.EqualTo(4));
        Assert.That(span[0], Is.EqualTo("a"));
        Assert.That(span[1], Is.EqualTo("e"));
        Assert.That(span[2], Is.EqualTo("c"));
        Assert.That(span[3], Is.EqualTo("d"));
    }

    [Test]
    public void Clear()
    {
        var freeList = new FreeList<string>(4);
        freeList.Add("a");
        freeList.Add("b");
        freeList.Add("c");
        freeList.Add("d");

        freeList.Clear();

        var span = freeList.AsSpan();
        Assert.That(span.Length, Is.EqualTo(0));
    }
}