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

    public sealed class TwoDimensionalArrayFormatter<T> : IMrbValueFormatter<T[,]?>
    {
        public T[,]? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var formatter = options.Resolver.GetFormatterWithVerify<T>();

            var list = new List<List<T>>();
            foreach (var x0 in mrbValue.AsArrayEnumerable())
            {
                var innerList1 = new List<T>();
                foreach (var x1 in x0.AsArrayEnumerable())
                {
                    innerList1.Add(formatter.Deserialize(x1, context, options));
                }
                list.Add(innerList1);
            }

            var result = new T[list.Count, list.Count > 0 ? list[0].Count : 0];
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = 0; j < list[i].Count; j++)
                {
                    result[i, j] = list[i][j];
                }
            }
            return result;
        }
    }

    public sealed class ThreeDimensionalArrayFormatter<T> : IMrbValueFormatter<T[,,]?>
    {
        public T[,,]? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var formatter = options.Resolver.GetFormatterWithVerify<T>();

            var list = new List<List<List<T>>>();
            foreach (var x0 in mrbValue.AsArrayEnumerable())
            {
                var innerList1 = new List<List<T>>();
                foreach (var x1 in x0.AsArrayEnumerable())
                {
                    var innerList2 = new List<T>();
                    foreach (var x2 in x1.AsArrayEnumerable())
                    {
                        innerList2.Add(formatter.Deserialize(x2, context, options));
                    }
                    innerList1.Add(innerList2);
                }
                list.Add(innerList1);
            }

            var length0 = list.Count;
            var length1 = length0 > 0 ? list[0].Count : 0;
            var length2 = length1 > 0 ? list[0][0].Count : 0;
            var result = new T[length0, length1, length2];
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = 0; j < list[i].Count; j++)
                {
                    for (var k = 0; k < list[i][j].Count; k++)
                    {
                        result[i, j, k] = list[i][j][k];
                    }
                }
            }

            return result;
        }
    }

    public sealed class FourDimensionalArrayFormatter<T> : IMrbValueFormatter<T[,,,]?>
    {
        public T[,,,]? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }

            var formatter = options.Resolver.GetFormatterWithVerify<T>();
            var list = new List<List<List<List<T>>>>();
            foreach (var x0 in mrbValue.AsArrayEnumerable())
            {
                var innerList1 = new List<List<List<T>>>();
                foreach (var x1 in x0.AsArrayEnumerable())
                {
                    var innerList2 = new List<List<T>>();
                    foreach (var x2 in x1.AsArrayEnumerable())
                    {
                        var innerList3 = new List<T>();
                        foreach (var x3 in x2.AsArrayEnumerable())
                        {
                            innerList3.Add(formatter.Deserialize(x3, context, options));
                        }
                        innerList2.Add(innerList3);
                    }
                    innerList1.Add(innerList2);
                }
                list.Add(innerList1);
            }

            var length0 = list.Count;
            var length1 = length0 > 0 ? list[0].Count : 0;
            var length2 = length1 > 0 ? list[0][0].Count : 0;
            var length3 = length2 > 0 ? list[0][0][0].Count : 0;
            var result = new T[length0, length1, length2, length3];
            for (var i = 0; i < list.Count; i++)
            {
                for (var j = 0; j < list[i].Count; j++)
                {
                    for (var k = 0; k < list[i][j].Count; k++)
                    {
                        for (var l = 0; l < list[i][j][k].Count; l++)
                        {
                            result[i, j, k, l] = list[i][j][k][l];
                        }
                    }
                }
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

    class StackFormatter<T> : IMrbValueFormatter<Stack<T>>
    {
        public Stack<T> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var list = new List<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                list.Add(formatter.Deserialize(x, context, options));
            }

            var stack = new Stack<T>();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                stack.Push(list[i]);
            }
            return stack;
        }
    }

    class QueueFormatter<T> : IMrbValueFormatter<Queue<T>>
    {
        public Queue<T> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var queue = new Queue<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                queue.Enqueue(formatter.Deserialize(x, context, options));
            }
            return queue;
        }
    }
}
