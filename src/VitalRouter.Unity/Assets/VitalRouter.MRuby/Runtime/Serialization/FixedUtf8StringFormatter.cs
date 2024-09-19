namespace VitalRouter.MRuby
{
    public class FixedUtf8StringFormatter : IMrbValueFormatter<FixedUtf8String>
    {
        public static readonly FixedUtf8StringFormatter Instance = new();

        public FixedUtf8String Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            return mrbValue.ToFixedUtf8String(context);
        }
    }
}