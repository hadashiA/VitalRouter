using System;

namespace VitalRouter.MRuby
{
    class TupleFormatter<T1> : IMrbValueFormatter<Tuple<T1>?>
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

    class TupleFormatter<T1, T2> : IMrbValueFormatter<Tuple<T1, T2>?>
    {
        public Tuple<T1, T2>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException($"An mruby array length is {length}. Cannot deserialize as `Tuple<T>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            return new Tuple<T1, T2>(item1, item2);
        }

        class TupleFormatter<T1, T2, T3> : IMrbValueFormatter<Tuple<T1, T2, T3>?>
        {
            public Tuple<T1, T2, T3>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
            {
                if (mrbValue.IsNil)
                {
                    return null;
                }

                var length = mrbValue.GetArrayLength();
                if (length < 2)
                {
                    throw new MRubySerializationException(
                        $"An mruby array length is {length}. Cannot deserialize as `Tuple<T>`.");
                }

                var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
                var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                    .Deserialize(item1Value, context, options);
                var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                    .Deserialize(item1Value, context, options);
                var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                    .Deserialize(item1Value, context, options);
                return new Tuple<T1, T2, T3>(item1, item2, item3);
            }
        }
    }
}