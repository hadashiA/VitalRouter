using System;

namespace VitalRouter.MRuby
{
    public class MRubySerializationException : Exception
    {
        public MRubySerializationException(string message) : base(message) { }

        public static void ThrowIfTypeMismatch(MrbValue mrbValue, MrbVtype expectedType, string? expectedTypeName = null, MRubyContext? context = null)
        {
            if (mrbValue.TT != expectedType)
            {
                var s = context != null ? mrbValue.ToString(context) : "";
                throw new MRubySerializationException(expectedTypeName != null
                    ? $"An mrb_value cannot convert to `{expectedTypeName}`. Expected={expectedType} Actual={mrbValue.TT}) `{s}`"
                    : $"An mrb_value is not an {expectedType}. ({mrbValue.TT})");
            }
        }

        public static void ThrowIfNotEnoughArrayLength(MrbValue mrbValue, int expectedLength, string? expectedTypeName = null, MRubyContext? context = null)
        {
            ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, expectedTypeName, context);
            var actualLength = mrbValue.GetArrayLength();
            if (actualLength < expectedLength)
            {
                throw new MRubySerializationException(expectedTypeName != null
                    ? $"An mrb_value cannot convert to `{expectedTypeName}`. The length of the array is not long enough. Expected={expectedLength} Actual={actualLength}"
                    : $"The length of the mruby array is not long enough. Expected={expectedLength} Actual={actualLength}");
            }
        }
    }
}