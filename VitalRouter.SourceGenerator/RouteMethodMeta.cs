using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VitalRouter.SourceGenerator;

class RouteMethodMetaEqualityComparer : IEqualityComparer<RouteMethodMeta>
{
    public static readonly RouteMethodMetaEqualityComparer Instance = new();

    public bool Equals(RouteMethodMeta x, RouteMethodMeta y)
    {
        return SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol);
    }

    public int GetHashCode(RouteMethodMeta obj)
    {
        return SymbolEqualityComparer.Default.GetHashCode(obj.Symbol);
    }
}

class RouteMethodMeta
{
    public IMethodSymbol Symbol { get; }
    public ITypeSymbol CommandTypeSymbol { get; }
    public InterceptorMeta[] InterceptorMetas { get; }
    public int SequentialOrder { get; }
    public string CommandFullTypeName { get; }
    public string CommandTypePrefix { get; }
    public bool TakeCancellationToken { get; }

    public bool IsAsync => Symbol.IsAsync || !Symbol.ReturnsVoid;

    public RouteMethodMeta(
        IMethodSymbol symbol,
        ITypeSymbol commandTypeSymbol,
        ReferenceSymbols references,
        int sequentialOrder)
    {
        Symbol = symbol;
        CommandTypeSymbol = commandTypeSymbol;
        SequentialOrder = sequentialOrder;

        InterceptorMetas = symbol.GetAttributes()
            .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, references.FilterAttribute) &&
                        x.ConstructorArguments is [{ Kind: TypedConstantKind.Type }, ..])
            .Select(x => new InterceptorMeta(x, (INamedTypeSymbol)x.ConstructorArguments[0].Value!))
            .ToArray();

        CommandFullTypeName = commandTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        CommandTypePrefix = CommandFullTypeName
            .Replace("global::", "")
            .Replace(".", "")
            .Replace("<", "_")
            .Replace(">", "_");
        commandTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (symbol.Parameters.Length > 1)
        {
            TakeCancellationToken = SymbolEqualityComparer.Default.Equals(symbol.Parameters[1].Type, references.CancellationTokenType);
        }
    }


    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        return Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
    }
}