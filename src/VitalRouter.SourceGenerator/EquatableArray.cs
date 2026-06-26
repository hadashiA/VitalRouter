using System;
using System.Collections;
using System.Collections.Generic;

namespace VitalRouter.SourceGenerator;

/// <summary>
/// A value-equatable wrapper around <typeparamref name="T"/>[].
/// Plain arrays use reference equality, which defeats the incremental generator
/// cache; this compares element-by-element so equal models compare equal.
/// </summary>
readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    readonly T[]? array;

    public EquatableArray(T[] array) => this.array = array;

    public T this[int index] => array![index];
    public int Count => array?.Length ?? 0;

    public bool Equals(EquatableArray<T> other)
    {
        if (array is null)
        {
            return other.array is null;
        }
        if (other.array is null)
        {
            return false;
        }
        if (array.Length != other.array.Length)
        {
            return false;
        }
        for (var i = 0; i < array.Length; i++)
        {
            if (!array[i].Equals(other.array[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (array is null)
        {
            return 0;
        }
        var hash = 17;
        foreach (var item in array)
        {
            hash = (hash * 31) + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public T[] ToArray() => array ?? Array.Empty<T>();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(array ?? Array.Empty<T>())).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator EquatableArray<T>(T[] array) => new(array);
}
