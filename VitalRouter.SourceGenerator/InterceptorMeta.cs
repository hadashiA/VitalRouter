using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

class InterceptorMetaEqualityComparer : IEqualityComparer<InterceptorMeta>
{
    public static readonly InterceptorMetaEqualityComparer Instance = new();

    public bool Equals(InterceptorMeta x, InterceptorMeta y)
    {
        return SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol);
    }

    public int GetHashCode(InterceptorMeta obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj.Symbol);
    }
}

class InterceptorMeta
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