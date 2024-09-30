using System;

namespace VitalRouter.MRuby
{
    public class ByteArrayFormatter : IMrbValueFormatter<byte[]?>
    {
        public static readonly ByteArrayFormatter Instance = new();

        public unsafe byte[]? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil) return null;
            if (mrbValue.TT == MrbVtype.MRB_TT_ARRAY)
            {
                return options.Resolver.GetFormatterWithVerify<byte[]>()
                    .Deserialize(mrbValue, context, options);
            }

            var s = NativeMethods.MrbToString(context.DangerousGetPtr(), mrbValue);
            var result = new byte[s.Length];
            var span = new Span<byte>(s.Bytes, s.Length);
            span.CopyTo(result);
            return result;
        }
    }
}