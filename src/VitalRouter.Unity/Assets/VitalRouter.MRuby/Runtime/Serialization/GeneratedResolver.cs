using System;
using System.Reflection;

namespace VitalRouter.MRuby
{
    public class GeneratedResolver : IMrbValueFormatterResolver
    {
        static class Check<T>
        {
            internal static bool Registered;
        }

        static class Cache<T>
        {
            internal static IMrbValueFormatter<T>? Formatter;

            static Cache()
            {
                if (Check<T>.Registered) return;

                var type = typeof(T);
                TryInvokeRegisterYamlFormatter(type);
            }
        }

        static bool TryInvokeRegisterYamlFormatter(Type type)
        {
            if (type.GetCustomAttribute<MRubyObjectAttribute>() == null) return false;

            var m = type.GetMethod("__RegisterMrbValueFormatter",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);

            if (m == null)
            {
                return false;
            }

            m.Invoke(null, null); // Cache<T>.formatter will set from method
            return true;
        }

        [Preserve]
        public static void Register<T>(IMrbValueFormatter<T> formatter)
        {
            Check<T>.Registered = true; // avoid to call Cache() constructor called.
            Cache<T>.Formatter = formatter;
        }

        public static readonly GeneratedResolver Instance = new();

        public IMrbValueFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;
    }
}
