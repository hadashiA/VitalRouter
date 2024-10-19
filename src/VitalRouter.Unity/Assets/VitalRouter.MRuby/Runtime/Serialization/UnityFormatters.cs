using Unity.Collections;
using UnityEngine;

namespace VitalRouter.MRuby
{
    class Vector2Formatter : IMrbValueFormatter<Vector2>
    {
        public static readonly Vector2Formatter Instance = new();

        public Vector2 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 2, "Vector2");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            return new Vector2(x, y);
        }
    }

    class Vector2IntFormatter : IMrbValueFormatter<Vector2Int>
    {
        public static readonly Vector2IntFormatter Instance = new();

        public Vector2Int Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 2, "Vector2Int");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<int>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            return new Vector2Int(x, y);
        }
    }

    class Vector3Formatter : IMrbValueFormatter<Vector3>
    {
        public static readonly Vector3Formatter Instance = new();

        public Vector3 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 3, "Vector3");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var z = formatter.Deserialize(zValue, context, options);
            return new Vector3(x, y, z);
        }
    }

    class Vector3IntFormatter : IMrbValueFormatter<Vector3Int>
    {
        public static readonly Vector3IntFormatter Instance = new();

        public Vector3Int Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 3, "Vector3Int");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);

            var formatter = options.Resolver.GetFormatterWithVerify<int>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var z = formatter.Deserialize(zValue, context, options);
            return new Vector3Int(x, y, z);
        }
    }

    class Vector4Formatter : IMrbValueFormatter<Vector4>
    {
        public static readonly Vector4Formatter Instance = new();

        public Vector4 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Vector4");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var z = formatter.Deserialize(zValue, context, options);
            var w = formatter.Deserialize(wValue, context, options);
            return new Vector4(x, y, z, w);
        }
    }

    class ColorFormatter : IMrbValueFormatter<Color>
    {
        public static readonly ColorFormatter Instance = new();

        public Color Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Color");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var r = formatter.Deserialize(xValue, context, options);
            var g = formatter.Deserialize(yValue, context, options);
            var b = formatter.Deserialize(zValue, context, options);
            var a = formatter.Deserialize(wValue, context, options);
            return new Color(r, g, b, a);
        }
    }

    class Color32Formatter : IMrbValueFormatter<Color32>
    {
        public static readonly Color32Formatter Instance = new();

        public Color32 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Color32");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<byte>();
            var r = formatter.Deserialize(xValue, context, options);
            var g = formatter.Deserialize(yValue, context, options);
            var b = formatter.Deserialize(zValue, context, options);
            var a = formatter.Deserialize(wValue, context, options);
            return new Color32(r, g, b, a);
        }
    }

    class QuaternionFormatter : IMrbValueFormatter<Quaternion>
    {
        public static readonly QuaternionFormatter Instance = new();

        public Quaternion Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Quaternion");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var zValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var z = formatter.Deserialize(zValue, context, options);
            var w = formatter.Deserialize(wValue, context, options);
            return new Quaternion(x, y, z, w);
        }
    }

    class Matrix4x4Formatter : IMrbValueFormatter<Matrix4x4>
    {
        public static readonly Matrix4x4Formatter Instance = new();

        public Matrix4x4 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Matrix4x4");

            var row0Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var row1Value = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var row2Value = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var row3Value = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var vector4Formatter = options.Resolver.GetFormatterWithVerify<Vector4>();
            var row0 = vector4Formatter.Deserialize(row0Value, context, options);
            var row1 = vector4Formatter.Deserialize(row1Value, context, options);
            var row2 = vector4Formatter.Deserialize(row2Value, context, options);
            var row3 = vector4Formatter.Deserialize(row3Value, context, options);

            var result = new Matrix4x4();
            result.SetRow(0, row0);
            result.SetRow(1, row1);
            result.SetRow(2, row2);
            result.SetRow(3, row3);
            return result;
        }
    }

    class ResolutionFormatter : IMrbValueFormatter<Resolution>
    {
        public static readonly ResolutionFormatter Instance = new();

        public Resolution Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 2, "Resolution");

            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var hValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<int>();
            var w = formatter.Deserialize(wValue, context, options);
            var h = formatter.Deserialize(hValue, context, options);
            return new Resolution { width = w, height = h };
        }
    }

    class BoundsFormatter : IMrbValueFormatter<Bounds>
    {
        public static readonly BoundsFormatter Instance = new();

        public Bounds Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 2, "Bounds");

            var centerValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var sizeValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<Vector3>();
            var center = formatter.Deserialize(centerValue, context, options);
            var size = formatter.Deserialize(sizeValue, context, options);
            return new Bounds(center, size);
        }
    }

    class BoundsIntFormatter : IMrbValueFormatter<BoundsInt>
    {
        public static readonly BoundsIntFormatter Instance = new();

        public BoundsInt Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 2, "BoundsInt");

            var centerValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var sizeValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<Vector3Int>();
            var center = formatter.Deserialize(centerValue, context, options);
            var size = formatter.Deserialize(sizeValue, context, options);
            return new BoundsInt(center, size);
        }
    }

    class RectFormatter : IMrbValueFormatter<Rect>
    {
        public static readonly RectFormatter Instance = new();

        public Rect Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "Rect");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var hValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var w = formatter.Deserialize(wValue, context, options);
            var h = formatter.Deserialize(hValue, context, options);
            return new Rect(x, y, w, h);
        }
    }

    class RectIntFormatter : IMrbValueFormatter<RectInt>
    {
        public static readonly RectIntFormatter Instance = new();

        public RectInt Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "RectInt");

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var wValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var hValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<int>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            var w = formatter.Deserialize(wValue, context, options);
            var h = formatter.Deserialize(hValue, context, options);
            return new RectInt(x, y, w, h);
        }
    }

    class RectOffsetFormatter : IMrbValueFormatter<RectOffset>
    {
        public static readonly RectOffsetFormatter Instance = new();

        public RectOffset Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfNotEnoughArrayLength(mrbValue, 4, "RectOffset");

            var lValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var rValue = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var tValue = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var bValue = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var formatter = options.Resolver.GetFormatterWithVerify<int>();
            var l = formatter.Deserialize(lValue, context, options);
            var r = formatter.Deserialize(rValue, context, options);
            var t = formatter.Deserialize(tValue, context, options);
            var b = formatter.Deserialize(bValue, context, options);
            return new RectOffset(l, r, t, b);
        }
    }

    class NativeArrayFormatter<T> : IMrbValueFormatter<NativeArray<T>> where T : struct
    {
        public NativeArray<T> Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, "NativeArray<>");

            var formatter = options.Resolver.GetFormatterWithVerify<T>();
            var length = mrbValue.GetArrayLength();
            var result = new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < length; i++)
            {
                var elementValue = NativeMethods.MrbArrayEntry(mrbValue, i);
                var element = formatter.Deserialize(elementValue, context, options);
                result[i] = element;
            }
            return result;

        }
    }
}
