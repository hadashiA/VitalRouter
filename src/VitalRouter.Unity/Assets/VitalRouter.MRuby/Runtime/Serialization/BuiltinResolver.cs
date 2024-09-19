using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VitalRouter.MRuby
{
    public class BuiltinResolver : IMrbValueFormatterResolver
    {
        static class FormatterCache<T>
        {
            public static readonly IMrbValueFormatter<T>? Formatter;

            static FormatterCache()
            {
                if (FormatterMap.TryGetValue(typeof(T), out var formatter))
                {
                    Formatter = (IMrbValueFormatter<T>)formatter;
                    return;
                }

                if (TryCreateGenericFormatter(typeof(T)) is IMrbValueFormatter<T> f)
                {
                    Formatter = f;
                    return;
                }

                Formatter = null;
            }
        }

        public static readonly BuiltinResolver Instance = new();

        static readonly Dictionary<Type, IMrbValueFormatter> FormatterMap = new()
        {
            // Primitive
            { typeof(Int16), Int16Formatters.Instance },
            { typeof(Int32), Int32Formatters.Instance },
            { typeof(Int64), Int64Formatter.Instance },
            { typeof(UInt16), UInt16Formatter.Instance },
            { typeof(UInt32), UInt32Formatter.Instance },
            { typeof(UInt64), UInt64Formatter.Instance },
            { typeof(Single), Float32Formatter.Instance },
            { typeof(Double), Float64Formatter.Instance },
            { typeof(bool), BooleanFormatter.Instance },
            { typeof(byte), ByteFormatter.Instance },
            { typeof(sbyte), SByteFormatter.Instance },
            { typeof(char), CharFormatter.Instance },
            // { typeof(DateTime), DateTimeFormatter.Instance },

            // StandardClassLibraryFormatter
            { typeof(string), NullableStringFormatter.Instance },
            { typeof(FixedUtf8String), FixedUtf8StringFormatter.Instance },
            { typeof(decimal), DecimalFormatter.Instance },
            // { typeof(TimeSpan), TimeSpanFormatter.Instance },
            // { typeof(DateTimeOffset), DateTimeOffsetFormatter.Instance },
            // { typeof(Guid), GuidFormatter.Instance },
            // { typeof(Uri), UriFormatter.Instance },
            // { typeof(Version), VersionFormatter.Instance },
            // { typeof(BitArray), BitArrayFormatter.Instance },
            // { typeof(Type), TypeFormatter.Instance },

            // well known collections

            // { typeof(Memory<byte>), ByteMemoryFormatter.Instance },
            // { typeof(ReadOnlyMemory<byte>), ByteReadOnlyMemoryFormatter.Instance },
            // { typeof(ReadOnlySequence<byte>), ByteReadOnlySequenceFormatter.Instance },
            // { typeof(ArraySegment<byte>), ByteArraySegmentFormatter.Instance },

            // { typeof(System.Numerics.BigInteger), BigIntegerFormatter.Instance },
            // { typeof(System.Numerics.Complex), ComplexFormatter.Instance },
        };

        public static readonly Dictionary<Type, Type> KnownGenericTypes = new()
        {
            { typeof(Tuple<>), typeof(TupleFormatter<>) },
            { typeof(ValueTuple<>), typeof(ValueTupleFormatter<>) },
            { typeof(Tuple<,>), typeof(TupleFormatter<,>) },
            { typeof(ValueTuple<,>), typeof(ValueTupleFormatter<,>) },
            { typeof(Tuple<,,>), typeof(TupleFormatter<,,>) },
            { typeof(ValueTuple<,,>), typeof(ValueTupleFormatter<,,>) },
            { typeof(Tuple<,,,>), typeof(TupleFormatter<,,,>) },
            { typeof(ValueTuple<,,,>), typeof(ValueTupleFormatter<,,,>) },
            { typeof(Tuple<,,,,>), typeof(TupleFormatter<,,,,>) },
            { typeof(ValueTuple<,,,,>), typeof(ValueTupleFormatter<,,,,>) },
            { typeof(Tuple<,,,,,>), typeof(TupleFormatter<,,,,,>) },
            { typeof(ValueTuple<,,,,,>), typeof(ValueTupleFormatter<,,,,,>) },
            { typeof(Tuple<,,,,,,>), typeof(TupleFormatter<,,,,,,>) },
            { typeof(ValueTuple<,,,,,,>), typeof(ValueTupleFormatter<,,,,,,>) },
            { typeof(Tuple<,,,,,,,>), typeof(TupleFormatter<,,,,,,,>) },
            { typeof(ValueTuple<,,,,,,,>), typeof(ValueTupleFormatter<,,,,,,,>) },

            { typeof(KeyValuePair<,>), typeof(KeyValuePairFormatter<,>) },
            // { typeof(Lazy<>), typeof(LazyFormatter<>) },
            { typeof(Nullable<>), typeof(NullableFormatter<>) },

            // { typeof(ArraySegment<>), typeof(ArraySegmentFormatter<>) },
            // { typeof(Memory<>), typeof(MemoryFormatter<>) },
            // { typeof(ReadOnlyMemory<>), typeof(ReadOnlyMemoryFormatter<>) },
            // { typeof(ReadOnlySequence<>), typeof(ReadOnlySequenceFormatter<>) },

            { typeof(List<>), typeof(ListFormatter<>) },
            { typeof(Stack<>), typeof(StackFormatter<>) },
            { typeof(Queue<>), typeof(QueueFormatter<>) },
            { typeof(LinkedList<>), typeof(LinkedListFormatter<>) },
            { typeof(HashSet<>), typeof(HashSetFormatter<>) },
            { typeof(SortedSet<>), typeof(SortedSetFormatter<>) },

            { typeof(Collection<>), typeof(CollectionFormatter<>) },
            { typeof(BlockingCollection<>), typeof(BlockingCollectionFormatter<>) },
            { typeof(ConcurrentQueue<>), typeof(ConcurrentQueueFormatter<>) },
            { typeof(ConcurrentStack<>), typeof(ConcurrentStackFormatter<>) },
            { typeof(ConcurrentBag<>), typeof(ConcurrentBagFormatter<>) },
            { typeof(Dictionary<,>), typeof(DictionaryFormatter<,>) },
            { typeof(SortedDictionary<,>), typeof(SortedDictionaryFormatter<,>) },
            { typeof(SortedList<,>), typeof(SortedListFormatter<,>) },
            { typeof(ConcurrentDictionary<,>), typeof(ConcurrentDictionaryFormatter<,>) },
            { typeof(ReadOnlyCollection<>), typeof(ReadOnlyCollectionFormatter<>) },

            { typeof(IEnumerable<>), typeof(InterfaceEnumerableFormatter<>) },
            { typeof(ICollection<>), typeof(InterfaceCollectionFormatter<>) },
            { typeof(IReadOnlyCollection<>), typeof(InterfaceReadOnlyCollectionFormatter<>) },
            { typeof(IList<>), typeof(InterfaceListFormatter<>) },
            { typeof(IReadOnlyList<>), typeof(InterfaceReadOnlyListFormatter<>) },
            { typeof(IDictionary<,>), typeof(InterfaceDictionaryFormatter<,>) },
            { typeof(IReadOnlyDictionary<,>), typeof(InterfaceReadOnlyDictionaryFormatter<,>) },
            { typeof(ISet<>), typeof(InterfaceSetFormatter<>) },
            { typeof(ILookup<,>), typeof(InterfaceLookupFormatter<,>) },
            { typeof(IGrouping<,>), typeof(InterfaceGroupingFormatter<,>) },
        };

        public IMrbValueFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        static object? TryCreateGenericFormatter(Type type)
        {
            Type? formatterType = null;

            if (type.IsArray)
            {
                if (type.IsSZArray)
                {
                    formatterType = typeof(ArrayFormatter<>).MakeGenericType(type.GetElementType()!);
                }
                else
                {
                    var rank = type.GetArrayRank();
                    switch (rank)
                    {
                        case 2:
                            formatterType = typeof(TwoDimensionalArrayFormatter<>).MakeGenericType(type.GetElementType()!);
                            break;
                        case 3:
                            formatterType = typeof(ThreeDimensionalArrayFormatter<>).MakeGenericType(type.GetElementType()!);
                            break;
                        case 4:
                            formatterType = typeof(FourDimensionalArrayFormatter<>).MakeGenericType(type.GetElementType()!);
                            break;
                        default:
                            break; // not supported
                    }
                }
            }
            else if (type.IsEnum)
            {
                formatterType = typeof(EnumAsStringFormatter<>).MakeGenericType(type);
            }
            else
            {
                formatterType = TryCreateGenericFormatterType(type, KnownGenericTypes);
            }

            if (formatterType != null)
            {
                return Activator.CreateInstance(formatterType);
            }
            return null;
        }

        static Type? TryCreateGenericFormatterType(Type type, IDictionary<Type, Type> knownTypes)
        {
            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();

                if (knownTypes.TryGetValue(genericDefinition, out var formatterType))
                {
                    return formatterType.MakeGenericType(type.GetGenericArguments());
                }
            }
            return null;
        }
    }

}