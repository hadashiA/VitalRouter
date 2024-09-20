using System;

namespace VitalRouter.MRuby
{
    public class MRubySerializationException : Exception
    {
        public MRubySerializationException(string message) : base(message) { }

        public static void ThrowIfTypeMismatch(MrbValue mrbValue, MrbVtype expectedType, string? expectedTypeName = null)
        {
            if (mrbValue.TT != expectedType)
            {
                throw new MRubySerializationException(expectedTypeName != null
                    ? $"An mrb_value cannot convert to `{expectedTypeName}`. Expected={expectedType} Actual={mrbValue.TT})"
                    : $"An mrb_value is not an {expectedType}. ({mrbValue.TT})");
            }
        }

        public static void ThrowIfNotEnoughArrayLength(MrbValue mrbValue, int expectedLength, string? expectedTypeName = null)
        {
            ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_ARRAY, expectedTypeName);
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