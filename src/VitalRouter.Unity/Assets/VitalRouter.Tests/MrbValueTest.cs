#if UNITY_2022_2_OR_NEWER
using NUnit.Framework;
using VitalRouter.MRuby;

namespace VitalRouter.Tests
{
    [TestFixture]
    public class MrbValueTest
    {
        [Test]
        public void Nil()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("nil").RawValue;
            Assert.That(value.IsNil, Is.True);
        }

        [Test]
        public void False()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("false").RawValue;
            Assert.That(value.IsFalse, Is.EqualTo(true));
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_FALSE));
        }

        [Test]
        public void True()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("true").RawValue;
            Assert.That(value.IsTrue, Is.EqualTo(true));
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_TRUE));
        }

        [Test]
        public void Symbol()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe(":abc").RawValue;
            Assert.That(value.IsSymbol, Is.EqualTo(true));
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_SYMBOL));
            Assert.That(value.ToString(context), Is.EqualTo("abc"));
        }

        [Test]
        public void String()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("'aiueo'").RawValue;
            Assert.That(value.IsObject, Is.EqualTo(true));
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_STRING));
            Assert.That(value.ToString(context), Is.EqualTo("aiueo"));
        }

        [Test]
        public void Int64Max()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("9223372036854775807").RawValue;
            Assert.That(value.IntValue, Is.EqualTo(9223372036854775807));
            Assert.That(value.IsFixnum, Is.False);
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_INTEGER));
        }

        [Test]
        public void Float()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("0.1234567").RawValue;
            Assert.That(value.FloatValue, Is.EqualTo(0.1234567));
            Assert.That(value.IsFloat, Is.True);
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_FLOAT));
        }

        [Test]
        public void Array()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("[1]").RawValue;
            Assert.That(value.IsObject, Is.True);
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_ARRAY));
        }

        [Test]
        public void Hash()
        {
            using var context = MRubyContext.Create();
            var value = context.EvaluateUnsafe("{ a: 1 }").RawValue;
            Assert.That(value.IsObject, Is.True);
            Assert.That(value.TT, Is.EqualTo(MrbVtype.MRB_TT_HASH));
        }
    }
}
#endif