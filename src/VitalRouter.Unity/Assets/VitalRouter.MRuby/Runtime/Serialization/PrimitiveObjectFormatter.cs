using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public class PrimitiveObjectFormatter : IMrbValueFormatter<object?>
    {
        public static readonly PrimitiveObjectFormatter Instance = new();

        public object? Deserialize(MrbValue value, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (value.IsNil)
            {
                return null;
            }

            switch (value.TT)
            {
                case MrbVtype.MRB_TT_FALSE:
                    return false;
                case MrbVtype.MRB_TT_TRUE:
                    return true;
                case MrbVtype.MRB_TT_INTEGER:
                    return (long)value.Value.I;
                case MrbVtype.MRB_TT_FLOAT:
                    return value.Value.F;
                case MrbVtype.MRB_TT_SYMBOL:
                case MrbVtype.MRB_TT_STRING:
                    return value.ToString(context);
                case MrbVtype.MRB_TT_ARRAY:
                {
                    var length = value.GetArrayLength();
                    var result = new object?[length];
                    for (var i = 0; i < length; i++)
                    {
                        var elementValue = NativeMethods.MrbArrayEntry(value, i);
                        var element = options.Resolver.GetFormatterWithVerify<object?>()
                            .Deserialize(elementValue, context, options);
                        result[i] = element;
                    }
                    return result;
                }
                case MrbVtype.MRB_TT_HASH:
                {
                    var dict = new Dictionary<object?, object?>();
                    foreach (var x in value.AsHashEnumerable(context))
                    {
                        var k = options.Resolver.GetFormatterWithVerify<string>()
                            .Deserialize(x.Key, context, options);
                        var v = options.Resolver.GetFormatterWithVerify<object?>()
                            .Deserialize(x.Value, context, options);
                        dict.Add(k!, v);
                    }
                    return dict;
                }
            }
            throw new MRubySerializationException($"Deserialization not supported `{{value.TT}}`");
        }
    }
}