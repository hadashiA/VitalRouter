namespace VitalRouter.Internal;

public static class UnsafeHelper
{
    public static ref TTo As<TFrom, TTo>(ref TFrom from)
    {
#if UNITY_2021_3_OR_NEWER
        return ref global::Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<TFrom, TTo>(ref from);
#else
        return ref System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref from);
#endif
    }
}
