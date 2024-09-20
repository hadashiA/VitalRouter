namespace VitalRouter.MRuby
{
    public static class MrbValueSerializer
    {
        public static T? Deserialize<T>(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions? options = null)
        {
            options ??= MrbValueSerializerOptions.Default;
            return options.Resolver.GetFormatterWithVerify<T>()
                .Deserialize(mrbValue, context, options);
       }
    }
}