using System.Runtime.CompilerServices;

namespace VitalRouter.Internal
{
static class UnsafeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo As<TFrom, TTo>(ref TFrom from) =>
#if UNITY_2021_3_OR_NEWER
        ref global::Unity.Collections.LowLevel.Unsafe.UnsafeUtility.As<TFrom, TTo>(ref from);
#else
        ref System.Runtime.CompilerServices.Unsafe.As<TFrom, TTo>(ref from);
#endif
}
}
