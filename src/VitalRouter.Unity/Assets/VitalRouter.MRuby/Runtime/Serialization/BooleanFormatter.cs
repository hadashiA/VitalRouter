namespace VitalRouter.MRuby
{
    public class BooleanFormatter : IMrbValueFormatter<bool>
    {
        public static readonly BooleanFormatter Instance = new();

        public bool Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                throw new MRubySerializationException("A mrb_value is nil. Cannot deserialize as bool.");
            }

            return mrbValue.TT switch
            {
                MrbVtype.MRB_TT_TRUE => true,
                MrbVtype.MRB_TT_FALSE => false,
                _ => throw new MRubySerializationException($"A mrb_value cannot deserialize as bool. {mrbValue.TT}")
            };
        }
    }
}
