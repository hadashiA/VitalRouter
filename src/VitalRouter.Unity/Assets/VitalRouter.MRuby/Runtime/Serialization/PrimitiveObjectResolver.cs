namespace VitalRouter.MRuby
{
    public class PrimitiveObjectResolver : IMrbValueFormatterResolver
    {
        public static readonly PrimitiveObjectResolver Instance = new();

        static class FormatterCache<T>
        {
            public static readonly IMrbValueFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMrbValueFormatter<T>)PrimitiveObjectFormatter.Instance;
            }
        }

        public IMrbValueFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }
    }

}