using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public class ArrayFormatter<T> : IMrbValueFormatter<T[]?>
    {
        public T[]? Deserialize(MrbValue value, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (value.IsNil)
            {
                return default;
            }

            if (value.TT != MrbVtype.MRB_TT_ARRAY)
            {
                throw new MRubySerializationException($"mrb_value is not an Array: {value.TT}");
            }

            var length = value.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(value, i);
                var element = options.Resolver.GetFormatterWithVerify<T>()
                    .Deserialize(elementValue, context, options);
                result[i] = element;
            }
            return result;
        }
    }

    public class ListFormatter<T> : IMrbValueFormatter<List<T>?>
    {
        public List<T>? Deserialize(MrbValue value, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (value.IsNil)
            {
                return default;
            }

            if (value.TT != MrbVtype.MRB_TT_ARRAY)
            {
                throw new MRubySerializationException($"mrb_value is not an Array: {value.TT}");
            }

            var length = value.GetArrayLength();
            var result = new List<T>(length);
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(value, i);
                var element = options.Resolver.GetFormatterWithVerify<T>()
                    .Deserialize(elementValue, context, options);
                result[i] = element;
            }
            return result;
        }
    }

    public class DictionaryFormatter<TKey, TValue> : IMrbValueFormatter<Dictionary<TKey, TValue>?> where TKey : notnull
    {
        public Dictionary<TKey, TValue>? Deserialize(MrbValue mrbValue,  MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetHashLength(context);
            var dict = new Dictionary<TKey, TValue?>(length);
            foreach (var x in mrbValue.AsHashEnumerable(context))
            {
                var key = options.Resolver.GetFormatterWithVerify<TKey>()
                    .Deserialize(x.Key, context, options);
                var value = options.Resolver.GetFormatterWithVerify<TValue>()
                    .Deserialize(x.Value, context, options);
                dict.Add(key, value);
            }
            return dict!;
        }
    }
}