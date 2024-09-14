using System;
using System.Buffers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VitalRouter.MRuby
{
    public static class NativeArrayExtensions
    {
#if !UNITY_2023_2_OR_NEWER
        public static unsafe Span<T> AsSpan<T>(this NativeArray<T> nativeArray) where T : unmanaged
        {
            return new Span<T>(nativeArray.GetUnsafePtr(), nativeArray.Length);
        }
#endif

        public static unsafe Memory<T> AsMemory<T>(this NativeArray<T> nativeArray) where T : unmanaged
        {
            return new NativeArrayMemoryManager<T>((T*)nativeArray.GetUnsafeReadOnlyPtr(), nativeArray.Length).Memory;
        }

        public static unsafe ReadOnlyMemory<T> AsMemory<T>(this NativeArray<T>.ReadOnly nativeArray) where T : unmanaged
        {
            return new NativeArrayMemoryManager<T>((T*)nativeArray.GetUnsafeReadOnlyPtr(), nativeArray.Length).Memory;
        }
    }

    unsafe class NativeArrayMemoryManager<T> : MemoryManager<T> where T : unmanaged
    {
        public T* Ptr { get; set; }
        public int Length { get; set; }

        public NativeArrayMemoryManager(NativeArray<T> nativeArray)
            : this((T*)nativeArray.GetUnsafeReadOnlyPtr(), nativeArray.Length)
        {
        }

        public NativeArrayMemoryManager(T* ptr, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            Ptr = ptr;
            Length = length;
        }

        public override Span<T> GetSpan() => new(Ptr, Length);

        /// <summary>
        /// Provides access to a pointer that represents the data (note: no actual pin occurs)
        /// </summary>
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (elementIndex < 0 || elementIndex >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(Length));
            }
            return new MemoryHandle(Ptr + elementIndex);
        }

        /// <summary>
        /// Has no effect
        /// </summary>
        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}