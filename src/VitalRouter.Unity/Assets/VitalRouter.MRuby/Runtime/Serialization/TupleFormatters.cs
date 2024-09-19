using System;

namespace VitalRouter.MRuby
{
    public class TupleFormatter<T1> : IMrbValueFormatter<Tuple<T1>?>
    {
        public Tuple<T1>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var length = mrbValue.GetArrayLength();
            if (length < 1)
            {
                throw new MRubySerializationException($"An mruby array length is {length}. Cannot deserialize as `Tuple<T>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            return new Tuple<T1>(item1!);
        }
    }
}