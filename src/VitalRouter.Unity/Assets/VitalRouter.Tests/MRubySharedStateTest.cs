#if UNITY_2022_2_OR_NEWER
using NUnit.Framework;
using VitalRouter.MRuby;

namespace VitalRouter.Tests
{
    [TestFixture]
    public class MRubySharedStateTest
    {
        [Test]
        public void GetSet()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            var sharedState = new MRubySharedState(context);

            sharedState.Set("string_value", "bar");
            Assert.That(sharedState.Get<string>("string_value"), Is.EqualTo("bar"));
            Assert.That(context.Evaluate<string>("state[:string_value]"), Is.EqualTo("bar"));

            sharedState.Set("int_value", 123);
            Assert.That(sharedState.Get<int>("int_value"), Is.EqualTo(123));
            Assert.That(context.Evaluate<int>("state[:int_value]"), Is.EqualTo(123));
        }

        [Test]
        public void Remove()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            var sharedState = new MRubySharedState(context);

            sharedState.Set("a", "hoge");
            sharedState.Set("b", "fuga");
            sharedState.Remove("a");
            Assert.That(sharedState.TryGet<string>("a", out _), Is.False);
            Assert.That(sharedState.TryGet<string>("b", out _), Is.True);
            Assert.That(context.Evaluate<string>("state[:a]"), Is.Null);
            Assert.That(context.Evaluate<string>("state[:b]"), Is.EqualTo("fuga"));
        }

        [Test]
        public void Clear()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            var sharedState = new MRubySharedState(context);

            sharedState.Set("string_value", "bar");
            sharedState.Clear();
            Assert.That(sharedState.TryGet<string>("string_value", out _), Is.False);
            Assert.That(context.Evaluate<string>("state[:string_value]"), Is.Null);
        }
    }
}
#endif