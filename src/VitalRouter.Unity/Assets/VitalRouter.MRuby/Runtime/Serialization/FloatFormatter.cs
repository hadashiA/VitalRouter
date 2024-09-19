namespace VitalRouter.MRuby
{
    public class Float32Formatter : IMrbValueFormatter<float>
    {
        public static readonly Float32Formatter Instance = new();

        public float Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_FLOAT)
            {
                throw new MRubySerializationException($"mrb_value is not a float: {mrbValue.TT}");
            }
            return (float)mrbValue.Value.F;
        }
    }

    public class Float64Formatter : IMrbValueFormatter<double>
    {
        public static readonly Float64Formatter Instance = new();

        public double Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_FLOAT)
            {
                throw new MRubySerializationException($"mrb_value is not a float: {mrbValue.TT}");
            }
            return mrbValue.Value.F;
        }
    }
}