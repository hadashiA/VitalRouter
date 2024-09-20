namespace VitalRouter.MRuby
{
    public class StandardResolver : IMrbValueFormatterResolver
    {
        public static readonly StandardResolver Instance = new();

        public static readonly IMrbValueFormatterResolver[] DefaultResolvers =
        {
            BuiltinResolver.Instance,
            UnityResolver.Instance,
            GeneratedResolver.Instance,
        };

        static class FormatterCache<T>
        {
            public static readonly IMrbValueFormatter<T>? Formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    // final fallback
                    Formatter = PrimitiveObjectResolver.Instance.GetFormatter<T>();
                }
                else
                {
                    foreach (var item in DefaultResolvers)
                    {
                        var f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            Formatter = f;
                            return;
                        }
                    }
                }
            }
        }

        public IMrbValueFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }
    }
}