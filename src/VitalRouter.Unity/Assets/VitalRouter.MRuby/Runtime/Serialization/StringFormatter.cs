namespace VitalRouter.MRuby
{
    public class NullableStringFormatter : IMrbValueFormatter<string?>
    {
        public static readonly NullableStringFormatter Instance = new();

        public string? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }
            return mrbValue.ToString(context);
        }
    }
}