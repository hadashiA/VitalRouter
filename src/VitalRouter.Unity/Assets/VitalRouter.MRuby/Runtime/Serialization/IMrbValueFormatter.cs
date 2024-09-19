namespace VitalRouter.MRuby
{
    public interface IMrbValueFormatter
    {
    }

    public interface IMrbValueFormatter<T>
    {
        T Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options);
    }
}
