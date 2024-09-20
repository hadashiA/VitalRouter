namespace VitalRouter.MRuby
{
    public class BooleanFormatter : IMrbValueFormatter<bool>
    {
        public static readonly BooleanFormatter Instance = new();

        public bool Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            return mrbValue.TT switch
            {
                MrbVtype.MRB_TT_TRUE => true,
                MrbVtype.MRB_TT_FALSE => false,
                _ => throw new MRubySerializationException($"A mrb_value cannot convert to bool. ({mrbValue.TT})")
            };
        }
    }
}
