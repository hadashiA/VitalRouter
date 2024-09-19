namespace VitalRouter.MRuby
{
    public interface IMrbValueFormatter
    {
    }

    public interface IMrbValueFormatter<T> : IMrbValueFormatter
    {
        T Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options);
    }
}
