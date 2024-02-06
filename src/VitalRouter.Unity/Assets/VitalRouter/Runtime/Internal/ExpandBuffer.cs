using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VitalRouter.Internal;

class ExpandBuffer<T> : IReadOnlyList<T>
{
    struct Enumerator : IEnumerator<T>
    {
        int currentIndex;
        ExpandBuffer<T> source;

        public Enumerator(ExpandBuffer<T> source)
        {
            this.source = source;
            currentIndex = -1;
        }

        public T Current => source[currentIndex];
        object IEnumerator.Current => Current!;

        public bool MoveNext() => ++currentIndex <= source.Count - 1;

        public void Reset()
        {
            currentIndex = -1;
        }

        public void Dispose() { }
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count { get; private set; }

    const int MinimumGrow = 4;
    const int GrowFactor = 200;

    T[] buffer;

    public ExpandBuffer(int capacity)
    {
        // buffer = ArrayPool<T>.Shared.Rent(capacity);
        buffer = new T[capacity];
        Count = 0;
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => buffer[index];
        set => buffer[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index) => ref buffer[index];

    public void CopyAndSetLengthTo(ExpandBuffer<T> dest)
    {
        if (Count <= dest.buffer.Length)
        {
            buffer.AsSpan(0, Count).CopyTo(dest.buffer.AsSpan(0, Count));
        }
        else
        {
            var newCapacity = Count * GrowFactor / 100;
            if (newCapacity < Count + MinimumGrow)
            {
                newCapacity = Count + MinimumGrow;
            }

            var newBuffer = new T[newCapacity];
            buffer.AsSpan(0, Count).CopyTo(newBuffer);
            dest.buffer = newBuffer;
        }
        dest.Count = Count;
    }

    public void RemoveAt(int index)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        Count--;
        if (index < Count)
        {
            Array.Copy(buffer, index + 1, buffer, index, Count - index);
        }
#if NETSTANDARD2_1_OR_GREATER
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
        {
            buffer[Count] = default!;
        }
    }

    public int IndexOf(T target)
    {
        return Array.IndexOf(buffer, target, 0, Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => buffer.AsSpan(0, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int length)
    {
        if (length > buffer.Length)
        {
            SetCapacity(buffer.Length * 2);
        }
        return buffer.AsSpan(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(bool clearElements = false)
    {
        Count = 0;
        if (clearElements)
        {
            Array.Clear(buffer, 0, Count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Peek() => ref buffer[Count - 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Pop()
    {
        if (Count == 0)
            throw new InvalidOperationException("Cannot pop the empty buffer");
        return ref buffer[--Count];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPop(out T value)
    {
        if (Count == 0)
        {
            value = default!;
            return false;
        }
        value = Pop();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (Count >= buffer.Length)
        {
            Grow();
        }

        buffer[Count++] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetCapacity(int newCapacity)
    {
        if (buffer.Length >= newCapacity) return;

        // var newBuffer = ArrayPool<T>.Shared.Rent(newCapacity);
        var newBuffer = new T[newCapacity];
        buffer.AsSpan(0, Count).CopyTo(newBuffer);
        // ArrayPool<T>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Grow()
    {
        var newCapacity = buffer.Length * GrowFactor / 100;
        if (newCapacity < buffer.Length + MinimumGrow)
        {
            newCapacity = buffer.Length + MinimumGrow;
        }
        SetCapacity(newCapacity);
    }
}
