namespace VitalRouter.MRuby
{
    public class NullableFormatter<T> : IMrbValueFormatter<T?> where T : struct
    {
        public T? Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return null;
            }
            return options.Resolver.GetFormatterWithVerify<T>()
                .Deserialize(mrbValue, context, options);
        }
    }
}
