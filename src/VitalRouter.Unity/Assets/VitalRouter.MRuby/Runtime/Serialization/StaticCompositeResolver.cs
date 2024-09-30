using System;
using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public class StaticCompositeResolver : IMrbValueFormatterResolver
    {
        public static readonly StaticCompositeResolver Instance = new();

        bool frozen;
        readonly List<IMrbValueFormatter> formatters = new();
        readonly List<IMrbValueFormatterResolver> resolvers = new();

        public StaticCompositeResolver AddFormatters(params IMrbValueFormatter[] formatters)
        {
            if (frozen)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            this.formatters.AddRange(formatters);
            return this;
        }

        public StaticCompositeResolver AddResolvers(params IMrbValueFormatterResolver[] resolvers)
        {
            if (frozen)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            this.resolvers.AddRange(resolvers);
            return this;
        }

        public IMrbValueFormatter<T>? GetFormatter<T>()
        {
            return Cache<T>.Formatter;
        }

        static class Cache<T>
        {
            public static readonly IMrbValueFormatter<T>? Formatter;

            static Cache()
            {
                Instance.frozen = true;
                foreach (var item in Instance.formatters)
                {
                    if (item is IMrbValueFormatter<T> f)
                    {
                        Formatter = f;
                        return;
                    }
                }

                foreach (var item in Instance.resolvers)
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
}