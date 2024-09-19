namespace VitalRouter.MRuby
{
    public class MrbValueSerializerOptions
    {
        public static MrbValueSerializerOptions Default => new()
        {
            Resolver = StandardResolver.Instance
        };

        public IMrbValueFormatterResolver Resolver { get; set; } = StandardResolver.Instance;
    }
}