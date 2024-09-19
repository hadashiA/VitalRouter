using UnityEngine;

namespace VitalRouter.MRuby
{
    class Vector2Formatter : IMrbValueFormatter<Vector2>
    {
        public static readonly Vector2Formatter Instance = new();

        public Vector2 Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            var length = mrbValue.GetArrayLength();
            if (length < 2)
            {
                throw new MRubySerializationException($"An mruby array length is {length}. Cannot deserialize as `Vector2<T>`.");
            }

            var xValue = NativeMethods.MrbArrayEntry(mrbValue, 0);
            var yValue = NativeMethods.MrbArrayEntry(mrbValue, 1);

            var formatter = options.Resolver.GetFormatterWithVerify<float>();
            var x = formatter.Deserialize(xValue, context, options);
            var y = formatter.Deserialize(yValue, context, options);
            return new Vector2(x, y);
        }
    }
}