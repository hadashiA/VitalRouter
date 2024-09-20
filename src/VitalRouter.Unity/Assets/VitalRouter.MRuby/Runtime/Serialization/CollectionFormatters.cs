using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VitalRouter.MRuby
{
    public class ArrayFormatter<T> : IMrbValueFormatter<T[]?>
    {
        public T[]? Deserialize(MrbValue value, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (value.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(value, MrbVtype.MRB_TT_ARRAY, "T[]", context);

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
            if (mrbValue.IsNil) return null;

            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "T[,]");
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
            if (mrbValue.IsNil) return null;

            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "T[,,]");
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
            if (mrbValue.IsNil) return null;

            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "T[,,,]");
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
            if (value.IsNil) return null;

            MRubySerializationException.ThrowIfTypeMismatch(value, MrbVtype.MRB_TT_ARRAY, "List<>");

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
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "Dictionary<>");

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

    class SortedDictionaryFormatter<TKey, TValue> : IMrbValueFormatter<SortedDictionary<TKey, TValue>?>
    {
        public SortedDictionary<TKey, TValue>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "SortedDictionary<>");

            var dict = new SortedDictionary<TKey, TValue?>();
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

    class ConcurrentDictionaryFormatter<TKey, TValue> : IMrbValueFormatter<ConcurrentDictionary<TKey, TValue>?>
    {
        public ConcurrentDictionary<TKey, TValue>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "ConcurrentDictionary<>");

            var dict = new ConcurrentDictionary<TKey, TValue?>();
            foreach (var x in mrbValue.AsHashEnumerable(context))
            {
                var key = options.Resolver.GetFormatterWithVerify<TKey>()
                    .Deserialize(x.Key, context, options);
                var value = options.Resolver.GetFormatterWithVerify<TValue>()
                    .Deserialize(x.Value, context, options);
                dict.TryAdd(key, value);
            }
            return dict!;
        }
    }

    public class InterfaceDictionaryFormatter<TKey, TValue> : IMrbValueFormatter<IDictionary<TKey, TValue>?> where TKey : notnull
    {
        public IDictionary<TKey, TValue>? Deserialize(MrbValue mrbValue,  MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "IDictionary<>");

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

    public class InterfaceReadOnlyDictionaryFormatter<TKey, TValue> : IMrbValueFormatter<IReadOnlyDictionary<TKey, TValue>?> where TKey : notnull
    {
        public IReadOnlyDictionary<TKey, TValue>? Deserialize(MrbValue mrbValue,  MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;

            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "IReadOnlyDictionary<>");
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

    class InterfaceEnumerableFormatter<T> : IMrbValueFormatter<IEnumerable<T>?>
    {
        public IEnumerable<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "IReadOnlyDictionary<>");
            var formatter = options.Resolver.GetFormatter<T>()!;
            var length = mrbValue.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                result[i] = formatter.Deserialize(elementValue, context, options);
            }
            return result;
        }
    }

    class InterfaceCollectionFormatter<T> : IMrbValueFormatter<ICollection<T>?>
    {
        public ICollection<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ICollection<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var length = mrbValue.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                result[i] = formatter.Deserialize(elementValue, context, options);
            }
            return result;
        }
    }

    class InterfaceReadOnlyCollectionFormatter<T> : IMrbValueFormatter<IReadOnlyCollection<T>?>
    {
        public IReadOnlyCollection<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "IReadOnlyCollection<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var length = mrbValue.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                result[i] = formatter.Deserialize(elementValue, context, options);
            }
            return result;
        }
    }

    class InterfaceListFormatter<T> : IMrbValueFormatter<IList<T>?>
    {
        public IList<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "IListCollection<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var length = mrbValue.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                result[i] = formatter.Deserialize(elementValue, context, options);
            }
            return result;
        }
    }

    class InterfaceReadOnlyListFormatter<T> : IMrbValueFormatter<IReadOnlyList<T>?>
    {
        public IReadOnlyList<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "IReadOnlyList<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var length = mrbValue.GetArrayLength();
            var result = new T[length];
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                result[i] = formatter.Deserialize(elementValue, context, options);
            }
            return result;
        }
    }

    class HashSetFormatter<T> : IMrbValueFormatter<HashSet<T>?>
    {
        public HashSet<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "HashSet<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var result = new HashSet<T>();
            foreach (var elementValue in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(elementValue, context, options));
            }
            return result;
        }
    }

    class SortedSetFormatter<T> : IMrbValueFormatter<SortedSet<T>?>
    {
        public SortedSet<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "SortedSet<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var result = new SortedSet<T>();
            foreach (var elementValue in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(elementValue, context, options));
            }
            return result;
        }
    }

    class InterfaceSetFormatter<T> : IMrbValueFormatter<ISet<T>?>
    {
        public ISet<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ISet<>");

            var formatter = options.Resolver.GetFormatter<T>()!;
            var result = new HashSet<T>();
            foreach (var elementValue in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(elementValue, context, options));
            }
            return result;
        }
    }

    class StackFormatter<T> : IMrbValueFormatter<Stack<T>?>
    {
        public Stack<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "Stack<>");

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

    class QueueFormatter<T> : IMrbValueFormatter<Queue<T>?>
    {
        public Queue<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "Queue<>");

            var queue = new Queue<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                queue.Enqueue(formatter.Deserialize(x, context, options));
            }
            return queue;
        }
    }

    class LinkedListFormatter<T> : IMrbValueFormatter<LinkedList<T>?>
    {
        public LinkedList<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "LinkedList<>");

            var result = new LinkedList<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                result.AddLast(formatter.Deserialize(x, context, options));
            }
            return result;
        }
    }

    class CollectionFormatter<T> : IMrbValueFormatter<Collection<T>?>
    {
        public Collection<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "Collection<>");

            var result = new Collection<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(x, context, options));
            }
            return result;
        }
    }

    class ReadOnlyCollectionFormatter<T> : IMrbValueFormatter<ReadOnlyCollection<T>?>
    {
        public ReadOnlyCollection<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ReadOnlyCollection<>");

            var list = new List<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                list.Add(formatter.Deserialize(x, context, options));
            }
            return new ReadOnlyCollection<T>(list);
        }
    }

    class BlockingCollectionFormatter<T> : IMrbValueFormatter<BlockingCollection<T>?>
    {
        public BlockingCollection<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "BlockingCollection<>");

            var result = new BlockingCollection<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(x, context, options));
            }
            return result;
        }
    }

    class ConcurrentQueueFormatter<T> : IMrbValueFormatter<ConcurrentQueue<T>?>
    {
        public ConcurrentQueue<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ConcurrentQueue<>");

            var result = new ConcurrentQueue<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                result.Enqueue(formatter.Deserialize(x, context, options));
            }
            return result;
        }
    }

    class ConcurrentStackFormatter<T> : IMrbValueFormatter<ConcurrentStack<T>?>
    {
        public ConcurrentStack<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ConcurrentStack<>");

            var list = new List<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                list.Add(formatter.Deserialize(x, context, options));
            }

            var stack = new ConcurrentStack<T>();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                stack.Push(list[i]);
            }
            return stack;
        }
    }

    class ConcurrentBagFormatter<T> : IMrbValueFormatter<ConcurrentBag<T>?>
    {
        public ConcurrentBag<T>? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "ConcurrentBag<>");

            var result = new ConcurrentBag<T>();
            var formatter = options.Resolver.GetFormatter<T>()!;
            foreach (var x in mrbValue.AsArrayEnumerable())
            {
                result.Add(formatter.Deserialize(x, context, options));
            }
            return result;
        }
    }
}
