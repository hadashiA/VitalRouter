using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace VitalRouter.MRuby
{
    public class UnityResolver : IMrbValueFormatterResolver
    {
        public static readonly UnityResolver Instance = new();

        static readonly Dictionary<Type, IMrbValueFormatter> FormatterMap = new()
        {
            { typeof(Color), ColorFormatter.Instance },
            { typeof(Color32), Color32Formatter.Instance },
            { typeof(Vector2), Vector2Formatter.Instance },
            { typeof(Vector2Int), Vector2IntFormatter.Instance },
            { typeof(Vector3), Vector3Formatter.Instance },
            { typeof(Vector3Int), Vector3IntFormatter.Instance },
            { typeof(Vector4), Vector4Formatter.Instance },
            { typeof(Matrix4x4), Matrix4x4Formatter.Instance },
            { typeof(Quaternion), QuaternionFormatter.Instance },

            { typeof(Resolution), ResolutionFormatter.Instance },

            // { typeof(Hash128), Hash128Formatter.Instance },

            { typeof(Bounds), BoundsFormatter.Instance },
            { typeof(BoundsInt), BoundsIntFormatter.Instance },
            // { typeof(Plane), PlaneFormatter.Instance },
            { typeof(Rect), RectFormatter.Instance },
            { typeof(RectInt), RectIntFormatter.Instance },
            { typeof(RectOffset), RectOffsetFormatter.Instance },
        };

        static readonly Dictionary<Type, Type> KnownGenericTypes = new()
        {
            { typeof(NativeArray<>), typeof(NativeArrayFormatter<>) }
        };

        public IMrbValueFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        static object? TryCreateGenericFormatter(Type type)
        {
            var formatterType = TryCreateGenericFormatterType(type, KnownGenericTypes);
            if (formatterType != null) return Activator.CreateInstance(formatterType);
            return null;
        }

        static Type? TryCreateGenericFormatterType(Type type, IDictionary<Type, Type> knownTypes)
        {
            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();

                if (knownTypes.TryGetValue(genericDefinition, out var formatterType))
                    return formatterType.MakeGenericType(type.GetGenericArguments());
            }

            return null;
        }

        static class FormatterCache<T>
        {
            public static readonly IMrbValueFormatter<T>? Formatter;

            static FormatterCache()
            {
                if (FormatterMap.TryGetValue(typeof(T), out var formatter) && formatter is IMrbValueFormatter<T> value)
                {
                    Formatter = value;
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
    }
}