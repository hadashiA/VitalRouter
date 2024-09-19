using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace VitalRouter.MRuby
{
    public interface IMrbValueFormatterResolver
    {
        IMrbValueFormatter<T>? GetFormatter<T>();
    }

    public static class MrbValueFormatterResolverExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMrbValueFormatter<T> GetFormatterWithVerify<T>(this IMrbValueFormatterResolver resolver)
        {
            IMrbValueFormatter<T>? formatter;
            try
            {
                formatter = resolver.GetFormatter<T>();
            }
            catch (TypeInitializationException ex)
            {
                // The fact that we're using static constructors to initialize this is an internal detail.
                // Rethrow the inner exception if there is one.
                // Do it carefully so as to not stomp on the original callstack.
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                return default!; // not reachable
            }

            if (formatter != null)
            {
                return formatter;
            }

            Throw(typeof(T), resolver);
            return default!; // not reachable
        }

        static void Throw(Type t, IMrbValueFormatterResolver resolver)
        {
            throw new MRubySerializationException(t.FullName + $"{t} is not registered in resolver: {resolver.GetType()}");
        }
    }
}