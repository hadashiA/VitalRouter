using System;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;

namespace VitalRouter.MRuby
{
    public unsafe struct FixedUtf8String : IEquatable<FixedUtf8String>
    {
        const int MaxLength = 64;

        public readonly int Length;
        fixed byte bytes[MaxLength];

        public FixedUtf8String(byte* bytes, int length)
        {
            if (length > MaxLength)
            {
                throw new ArgumentException($"String is too long. length: {length}");
            }
            fixed (byte* ptr = this.bytes)
            {
                UnsafeUtility.MemCpy(ptr, bytes, length);
            }
            Length = length;
        }

        public FixedUtf8String(string str)
        {
            var maxByteCount = Encoding.UTF8.GetMaxByteCount(str.Length);
            Span<byte> buf = stackalloc byte[maxByteCount];
            var written = Encoding.UTF8.GetBytes(str, buf);
            if (written > MaxLength)
            {
                throw new ArgumentException($"String is too long: {str}");
            }

            fixed (byte* ptr = bytes)
            {
                buf[..written].CopyTo(new Span<byte>(ptr, written));
            }
            Length = written;
        }

        public bool EquivalentIgnoreCaseTo(ReadOnlySpan<byte> other)
        {
            fixed (byte* ptr = bytes)
            {
                var span = new ReadOnlySpan<byte>(ptr, Length);
                if (span.SequenceEqual(other))
                {
                    return true;
                }

                // Test to underscore
                Span<byte> otherUnderscore = stackalloc byte[other.Length * 2];
                int written;
                while (!NamingConventionMutator.SnakeCase.TryMutate(other, otherUnderscore, out written))
                {
                    // ReSharper disable once StackAllocInsideLoop
                    otherUnderscore = stackalloc byte[otherUnderscore.Length * 2];
                }
                if (span.SequenceEqual(otherUnderscore[..written]))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            fixed (byte* ptr = bytes)
            {
                return Encoding.UTF8.GetString(ptr, Length);
            }
        }

        public bool Equals(FixedUtf8String other)
        {
            if (Length != other.Length) return false;

            fixed (byte* ptr = bytes)
            {
                var span = new ReadOnlySpan<byte>(ptr, Length);
                var otherSpan = new ReadOnlySpan<byte>(other.bytes, other.Length);
                return span.SequenceEqual(otherSpan);
            }
        }

        public override bool Equals(object? other)
        {
            if (other is FixedUtf8String otherS)
            {
                return Equals(otherS);
            }
            return false;
        }

        public override int GetHashCode()
        {
            var n = Length;
            ulong hash = 5381;
            while (n > 0)
            {
                ulong c = bytes[--n];
                hash = (hash << 5) + hash + c;
            }
            return (int)hash;
        }
    }
}
