using System.Collections.Generic;

namespace VitalRouter.Internal;

#if !NETSTANDARD2_1_OR_GREATER
public static class Shims
{
    public static bool TryDequeue<T>(this Queue<T> queue, out T result)
    {
        if (queue.Count <= 0)
        {
            result = default!;
            return false;
        }
        result = queue.Dequeue();
        return true;
    }
}
#endif