using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VitalRouter.Internal
{
public class FreeList<T> where T : class
{
    public bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LastIndex == -2;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LastIndex < 0;
    }

    readonly object gate = new();
    internal T?[] Values;
    internal int LastIndex = -1;

    public FreeList(int initialCapacity)
    {
        Values = new T[initialCapacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T?> AsSpan() => LastIndex >= 0
        ? Values.AsSpan(0, LastIndex + 1)
        : ReadOnlySpan<T?>.Empty;

    public T? this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Values[index];
    }

    public void Add(T item)
    {
        lock (gate)
        {
            CheckDispose();

            // try find blank
            var index = FindNullIndex(Values);
            if (index == -1)
            {
                // full, 1, 4, 6,...resize(x1.5)
                var len = Values.Length;
                var newValues = new T[len + len / 2];
                Array.Copy(Values, newValues, len);
                Values = newValues;
                index = len;
            }

            Values[index] = item;
            if (LastIndex < index)
            {
                LastIndex = index;
            }
        }
    }

    public void RemoveAt(int index)
    {
        lock (gate)
        {
            if (index < Values.Length)
            {
                ref var v = ref Values[index];
                if (v == null) throw new KeyNotFoundException($"key index {index} is not found.");

                v = null;
                if (index == LastIndex)
                {
                    LastIndex = FindLastNonNullIndex(Values, index);
                }
            }
        }
    }

    public bool Remove(T value)
    {
        lock (gate)
        {
            if (LastIndex < 0) return false;

            var index = -1;
            var span = Values.AsSpan(0, LastIndex + 1);
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == value)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
        }
        return false;
    }

    public void Clear()
    {
        lock (gate)
        {
            if (LastIndex > -1)
            {
                Values.AsSpan(0, LastIndex + 1).Clear();
                LastIndex = -1;
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            LastIndex = -2; // -2 is disposed.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void CheckDispose()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

#if NET6_0_OR_GREATER

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int FindNullIndex(T?[] target)
    {
        var span = MemoryMarshal.CreateReadOnlySpan(
            ref UnsafeHelper.As<T?, IntPtr>(ref MemoryMarshal.GetArrayDataReference(target)), target.Length);
        return span.IndexOf(IntPtr.Zero);
    }

#else

    static unsafe int FindNullIndex(T?[] target)
    {
        ref var head = ref UnsafeHelper.As<T?, IntPtr>(ref MemoryMarshal.GetReference(target.AsSpan()));
        fixed (void* p = &head)
        {
            var span = new ReadOnlySpan<IntPtr>(p, target.Length);

#if NETSTANDARD2_1
            return span.IndexOf(IntPtr.Zero);
#else
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == IntPtr.Zero) return i;
            }
            return -1;
#endif
        }
    }

#endif

#if NET8_0_OR_GREATER

    static int FindLastNonNullIndex(T?[] target, int lastIndex)
    {
        var span = MemoryMarshal.CreateReadOnlySpan(
            ref UnsafeHelper.As<T?, IntPtr>(ref MemoryMarshal.GetArrayDataReference(target)), lastIndex); // without lastIndexed value.
        var index = span.LastIndexOfAnyExcept(IntPtr.Zero);
        return index; // return -1 is ok(means empty)
    }

#else

    static unsafe int FindLastNonNullIndex(T?[] target, int lastIndex)
    {
        ref var head = ref UnsafeHelper.As<T?, IntPtr>(ref MemoryMarshal.GetReference(target.AsSpan()));
        fixed (void* p = &head)
        {
            var span = new ReadOnlySpan<IntPtr>(p, lastIndex); // without lastIndexed value.

            for (var i = span.Length - 1; i >= 0; i--)
            {
                if (span[i] != IntPtr.Zero) return i;
            }

            return -1;
        }
    }

#endif
}
}
