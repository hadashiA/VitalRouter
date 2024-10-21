namespace VitalRouter.MRuby
{
    public class ByteFormatter : IMrbValueFormatter<byte>
    {
        public static readonly ByteFormatter Instance = new();

        public byte Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((byte)mrbValue.IntValue);
        }
    }

    public class SByteFormatter : IMrbValueFormatter<sbyte>
    {
        public static readonly SByteFormatter Instance = new();

        public sbyte Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((sbyte)mrbValue.IntValue);
        }
    }

    public class CharFormatter : IMrbValueFormatter<char>
    {
        public static readonly CharFormatter Instance = new();

        public char Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((char)mrbValue.IntValue);
        }
    }

    public class Int16Formatters : IMrbValueFormatter<short>
    {
        public static readonly Int16Formatters Instance = new();

        public short Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((short)mrbValue.IntValue);
        }
    }

    public class Int32Formatters : IMrbValueFormatter<int>
    {
        public static readonly Int32Formatters Instance = new();

        public int Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((int)mrbValue.IntValue);
        }
    }

    public class Int64Formatter : IMrbValueFormatter<long>
    {
        public static readonly Int64Formatter Instance = new();

        public long Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((long)mrbValue.IntValue);
        }
    }

    public class UInt16Formatter : IMrbValueFormatter<ushort>
    {
        public static readonly UInt16Formatter Instance = new();

        public ushort Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((ushort)mrbValue.IntValue);
        }
    }

    public class UInt32Formatter : IMrbValueFormatter<uint>
    {
        public static readonly UInt32Formatter Instance = new();

        public uint Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((uint)mrbValue.IntValue);
        }
    }

    public class UInt64Formatter : IMrbValueFormatter<ulong>
    {
        public static readonly UInt64Formatter Instance = new();

        public ulong Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_INTEGER)
            {
                throw new MRubySerializationException($"value is not a Integer. {mrbValue.TT}");
            }
            return checked((ulong)mrbValue.IntValue);
        }
    }
}