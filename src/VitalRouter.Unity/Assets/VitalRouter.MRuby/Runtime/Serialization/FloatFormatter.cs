namespace VitalRouter.MRuby
{
    public class Float32Formatter : IMrbValueFormatter<float>
    {
        public static readonly Float32Formatter Instance = new();

        public float Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            return mrbValue.TT switch
            {
                MrbVtype.MRB_TT_FLOAT => (float)mrbValue.Value.F,
                MrbVtype.MRB_TT_INTEGER => mrbValue.Value.I,
                _ => throw new MRubySerializationException($"mrb_value cannot deserialize as float: {mrbValue.TT}")
            };
        }
    }

    public class Float64Formatter : IMrbValueFormatter<double>
    {
        public static readonly Float64Formatter Instance = new();

        public double Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            return mrbValue.TT switch
            {
                MrbVtype.MRB_TT_FLOAT => (float)mrbValue.Value.F,
                MrbVtype.MRB_TT_INTEGER => mrbValue.Value.I,
                _ => throw new MRubySerializationException($"mrb_value cannot deserialize as double: {mrbValue.TT}")
            };
        }
    }

    public class DecimalFormatter : IMrbValueFormatter<decimal>
    {
        public static readonly DecimalFormatter Instance = new();

        public decimal Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            return mrbValue.TT switch
            {
                MrbVtype.MRB_TT_FLOAT => (decimal)mrbValue.Value.F,
                MrbVtype.MRB_TT_INTEGER => mrbValue.Value.I,
                _ => throw new MRubySerializationException($"mrb_value cannot deserialize as decimal: {mrbValue.TT}")
            };
        }
    }
}