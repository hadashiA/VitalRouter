using System;
using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    class KeyValuePairFormatter<TKey, TValue> : IMrbValueFormatter<KeyValuePair<TKey, TValue>>
    {
        public KeyValuePair<TKey, TValue> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var key = options.Resolver.GetFormatterWithVerify<TKey>()
                .Deserialize(item1Value, context, options);
            var value = options.Resolver.GetFormatterWithVerify<TValue>()
                .Deserialize(item1Value, context, options);
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

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
                throw new MRubySerializationException($"An mruby array length is {length}. Cannot deserialize as `Tuple<>`.");
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
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            return new Tuple<T1, T2>(item1, item2);
        }
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
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,>`.");
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

    class TupleFormatter<T1, T2, T3, T4> : IMrbValueFormatter<Tuple<T1, T2, T3, T4>?>
    {
        public Tuple<T1, T2, T3, T4>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                .Deserialize(item1Value, context, options);
            var item4 = options.Resolver.GetFormatterWithVerify<T4>()
                .Deserialize(item1Value, context, options);
            return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }
    }

    class TupleFormatter<T1, T2, T3, T4, T5> : IMrbValueFormatter<Tuple<T1, T2, T3, T4, T5>?>
    {
        public Tuple<T1, T2, T3, T4, T5>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                .Deserialize(item1Value, context, options);
            var item4 = options.Resolver.GetFormatterWithVerify<T4>()
                .Deserialize(item1Value, context, options);
            var item5 = options.Resolver.GetFormatterWithVerify<T5>()
                .Deserialize(item1Value, context, options);
            return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }
    }

    class ValueTupleFormatter<T1> : IMrbValueFormatter<ValueTuple<T1>>
    {
        public ValueTuple<T1> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 1)
            {
                throw new MRubySerializationException($"An mruby array length is {length}. Cannot deserialize as `ValueTuple<>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            return new ValueTuple<T1>(item1!);
        }
    }

    class ValueTupleFormatter<T1, T2> : IMrbValueFormatter<ValueTuple<T1, T2>>
    {
        public ValueTuple<T1, T2> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            return new ValueTuple<T1, T2>(item1, item2);
        }
    }

    class ValueTupleFormatter<T1, T2, T3> : IMrbValueFormatter<ValueTuple<T1, T2, T3>>
    {
        public ValueTuple<T1, T2, T3> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                .Deserialize(item1Value, context, options);
            return new ValueTuple<T1, T2, T3>(item1, item2, item3);
        }
    }

    class ValueTupleFormatter<T1, T2, T3, T4> : IMrbValueFormatter<ValueTuple<T1, T2, T3, T4>>
    {
        public ValueTuple<T1, T2, T3, T4> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                .Deserialize(item1Value, context, options);
            var item4 = options.Resolver.GetFormatterWithVerify<T4>()
                .Deserialize(item1Value, context, options);
            return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }
    }

    class ValueTupleFormatter<T1, T2, T3, T4, T5> : IMrbValueFormatter<ValueTuple<T1, T2, T3, T4, T5>>
    {
        public ValueTuple<T1, T2, T3, T4, T5> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException(
                    $"An mruby array length is {length}. Cannot deserialize as `Tuple<,,,>`.");
            }

            var item1Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var item1 = options.Resolver.GetFormatterWithVerify<T1>()
                .Deserialize(item1Value, context, options);
            var item2 = options.Resolver.GetFormatterWithVerify<T2>()
                .Deserialize(item1Value, context, options);
            var item3 = options.Resolver.GetFormatterWithVerify<T3>()
                .Deserialize(item1Value, context, options);
            var item4 = options.Resolver.GetFormatterWithVerify<T4>()
                .Deserialize(item1Value, context, options);
            var item5 = options.Resolver.GetFormatterWithVerify<T5>()
                .Deserialize(item1Value, context, options);
            return new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }
    }
}