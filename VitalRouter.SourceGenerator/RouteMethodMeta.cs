using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VitalRouter.SourceGenerator;

class RouteMethodMeta
{
    public IMethodSymbol Symbol { get; }
    public ITypeSymbol CommandTypeSymbol { get; }
    public INamedTypeSymbol[] FilterTypeSymbols { get; }
    public int SequentialOrder { get; }
    public string CommandFullTypeName { get; }
    public bool TakeCancellationToken { get; }

    public RouteMethodMeta(
        IMethodSymbol symbol,
        ITypeSymbol commandTypeSymbol,
        ReferenceSymbols references,
        int sequentialOrder)
    {
        Symbol = symbol;
        CommandTypeSymbol = commandTypeSymbol;
        SequentialOrder = sequentialOrder;

        FilterTypeSymbols = symbol.GetAttributes()
            .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, references.FilterAttribute) &&
                        x.ConstructorArguments is [{ Kind: TypedConstantKind.Type }, ..])
            .Select(x => (INamedTypeSymbol)x.ConstructorArguments[0].Value!)
            .ToArray();

        CommandFullTypeName = commandTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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