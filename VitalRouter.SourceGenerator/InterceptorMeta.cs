using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

public class InterceptorMeta
{
    public INamedTypeSymbol Symbol { get; }
    public string FullTypeName { get; }
    public string VariableName { get; }

    public InterceptorMeta(INamedTypeSymbol symbol)
    {
        Symbol = symbol;
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        VariableName = FullTypeName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_");

        if (char.IsUpper(VariableName[0]))
        {
            VariableName = char.ToLower(VariableName[0]) + VariableName[1..];
        }
    }
}