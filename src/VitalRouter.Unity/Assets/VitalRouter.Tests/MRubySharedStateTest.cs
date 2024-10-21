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

            sharedState.Set("symbol_value", "bra_bra", asSymbol: true);
            Assert.That(sharedState.Get<string>("symbol_value"), Is.EqualTo("bra_bra"));
            Assert.That(context.Evaluate<string>("state[:symbol_value]"), Is.EqualTo("bra_bra"));

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

        [Test]
        public void FuzzyMatcher()
        {
            using var context = MRubyContext.Create(Router.Default, new DummyCommandPreset());
            var sharedState = new MRubySharedState(context);

            sharedState.Set("a", "bar");
            sharedState.Set("b", 123);
            sharedState.Set("c", 1234.0f);
            sharedState.Set("d", true);
            sharedState.Set("e", false);

            Assert.That(context.Evaluate<bool>("state[:a].is?(:bar)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:a].is?('bar')"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:b].is?(123)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:c].is?(1234.0)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:d].is?(true)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:e].is?(false)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:e].is?(nil)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:e].is?(true)"), Is.False);
            Assert.That(context.Evaluate<bool>("state[:f].is?(nil)"), Is.True);
            Assert.That(context.Evaluate<bool>("state[:f].is?(false)"), Is.True);
        }
    }
}
#endif