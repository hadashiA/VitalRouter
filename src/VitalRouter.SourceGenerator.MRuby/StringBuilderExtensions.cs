using System.Text;

namespace VitalRouter.SourceGenerator.MRuby;

public static class StringBuilderExtensions
{
    public static void AppendByteArrayString(this StringBuilder stringBuilder, byte[] bytes)
    {
        stringBuilder.Append("{ ");
        var first = true;
        foreach (var x in bytes)
        {
            if (!first)
            {
                stringBuilder.Append(", ");
            }
            stringBuilder.Append(x);
            first = false;
        }
        stringBuilder.Append(" }");
    }
}
