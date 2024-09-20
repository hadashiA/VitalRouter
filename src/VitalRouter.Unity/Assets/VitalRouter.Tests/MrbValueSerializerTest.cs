#if UNITY_2022_2_OR_NEWER
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VitalRouter.MRuby;

namespace VitalRouter.Tests
{
    class DummyCommandPreset : MRubyCommandPreset
    {
        public override UniTask CommandCallFromMrubyAsync(MRubyScript script, FixedUtf8String commandName, MrbValue payload,
            CancellationToken cancellation = default)
        {
            throw new System.NotImplementedException();
        }
    }

    enum DummyEnum
    {
        Foo,
        BarBuz,
    }

    [MRubyObject]
    partial class MRubySerializableClass
    {
        public int X { get; set; }
        public string[] Array { get; set; }
        public Dictionary<string, MRubyStruct> Dict { get; set; }

        [MRubyMember("alias_of_y")]
        public int Y { get; set; }
    }

    [MRubyObject]
    partial class MRubyConstructorClass
    {
        public int X { get; }
        public int Y { get; }
        public string Hoge { get; }

        [MRubyConstructor]
        public MRubyConstructorClass(int x, int y, string hoge)
        {
            X = x;
            Y = y;
            Hoge = hoge;
        }
    }

    [MRubyObject]
    partial struct MRubyStruct
    {
        public long Id { get; set; }
    }

    [TestFixture]
    public class MrbValueSerializerTest
    {
        [Test]
        public void Deserialize_Primitive()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            Assert.That(context.Evaluate<int>("12345"), Is.EqualTo(12345));
            Assert.That(context.Evaluate<float>("1.23"), Is.EqualTo(1.23f));
            Assert.That(context.Evaluate<double>("1.23"), Is.EqualTo(1.23).Within(0.001));
            Assert.That(context.Evaluate<bool>("true"), Is.True);
            Assert.That(context.Evaluate<bool>("false"), Is.False);

            context.Load("def unchi = 1 + 1");
            Assert.That(context.Evaluate<int>("unchi"), Is.EqualTo(2));
        }

        [Test]
        public void Deserialize_Enum()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            Assert.That(context.Evaluate<DummyEnum>("'Foo'"), Is.EqualTo(DummyEnum.Foo));
        }

        [Test]
        public void Deserialize_UnityTypes()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            Assert.That(context.Evaluate<Vector2>("[123, 456]"), Is.EqualTo(new Vector2(123, 456)));
        }

        [Test]
        public void Deserialize_Generated()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());

            var result1 = context.Evaluate<MRubySerializableClass>(
                "{ x: 1233, alias_of_y: 999, array: %w(hey hoi yo), dict: { a: { id: 100 }, b: { id: 200 } } }")!;

            Assert.That(result1.X, Is.EqualTo(1233));
            Assert.That(result1.Y, Is.EqualTo(999));
            Assert.That(result1.Array, Is.EquivalentTo(new[] { "hey", "hoi", "yo" }));
            Assert.That(result1.Dict["a"].Id, Is.EqualTo(100));
            Assert.That(result1.Dict["b"].Id, Is.EqualTo(200));

            var result2 = context.Evaluate<MRubyConstructorClass>(
                "{ x: 123, y: 999, hoge: 'aaa' }")!;
            Assert.That(result2.X, Is.EqualTo(123));
            Assert.That(result2.Y, Is.EqualTo(999));
            Assert.That(result2.Hoge, Is.EqualTo("aaa"));
        }
    }
}
#endif