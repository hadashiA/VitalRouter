using System;
using System.Text;

namespace VitalRouter.MRuby
{
    public unsafe struct FixedUtf8String : IEquatable<FixedUtf8String>
    {
        const int MaxLength = 64;

        public readonly int Length;
        fixed byte bytes[MaxLength];

        // this.bytes is not GC target, it is located as a value on this struct
        public ReadOnlySpan<byte> AsSpan()
        {
            fixed (byte* ptr = bytes)
            {
                return new ReadOnlySpan<byte>(ptr, Length);
            }
        }

        public FixedUtf8String(byte* bytes, int length)
        {
            if (length > MaxLength)
            {
                throw new ArgumentException($"String is too long. length: {length}");
            }
            var source = new ReadOnlySpan<byte>(bytes, length);
            fixed (byte* ptr = this.bytes)
            {
                source.CopyTo(new Span<byte>(ptr, length));
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

        public override string ToString() => Encoding.UTF8.GetString(AsSpan());

        public bool Equals(FixedUtf8String other)
        {
            return AsSpan().SequenceEqual(other.AsSpan());
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
