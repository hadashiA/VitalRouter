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

            var col0Value = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var col1Value = NativeMethods.MrbArrayEntry(mrbValue, 1);
            var col2Value = NativeMethods.MrbArrayEntry(mrbValue, 2);
            var col3Value = NativeMethods.MrbArrayEntry(mrbValue, 3);

            var vector4Formatter = options.Resolver.GetFormatterWithVerify<Vector4>();
            var col0 = vector4Formatter.Deserialize(col0Value, context, options);
            var col1 = vector4Formatter.Deserialize(col1Value, context, options);
            var col2 = vector4Formatter.Deserialize(col2Value, context, options);
            var col3 = vector4Formatter.Deserialize(col3Value, context, options);
            return new Matrix4x4(col0, col1, col2, col3);
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
}
