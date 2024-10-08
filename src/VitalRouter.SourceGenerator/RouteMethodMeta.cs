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
    public bool TakePublishContext { get; }

    public bool IsAsync => Symbol.IsAsync || !Symbol.ReturnsVoid;
    public bool IsValueTask() => SymbolEqualityComparer.Default.Equals(Symbol.ReturnType, references.ValueTaskType);
    public bool IsUniTask() => SymbolEqualityComparer.Default.Equals(Symbol.ReturnType, references.UniTaskType);

    readonly ReferenceSymbols references;

    public RouteMethodMeta(
        IMethodSymbol symbol,
        IParameterSymbol commandParamSymbol,
        IParameterSymbol? cancellationTokenParamSymbol,
        IParameterSymbol? publishContextParamSymbol,
        int sequentialOrder,
        ReferenceSymbols references,
        AttributeData? routeAttribute)
    {
        Symbol = symbol;
        CommandTypeSymbol = commandParamSymbol.Type;
        SequentialOrder = sequentialOrder;

        var interceptorMetas = new List<InterceptorMeta>();

        if (routeAttribute is { ConstructorArguments.Length: > 0 } &&
            routeAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Enum &&
            routeAttribute.ConstructorArguments[0].Value is int intValue and > 0)
        {
            interceptorMetas.Add(new InterceptorMeta(routeAttribute, (CommandOrdering)intValue, references));
        }

        foreach (var attr in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, references.FilterAttribute) &&
                attr.ConstructorArguments is [{ Kind: TypedConstantKind.Type }, ..])
            {
                interceptorMetas.Add(new InterceptorMeta(attr, (INamedTypeSymbol)attr.ConstructorArguments[0].Value!));
            }
            else if (attr.AttributeClass is { IsGenericType: true } x &&
                     SymbolEqualityComparer.Default.Equals(x.ConstructedFrom, references.FilterAttributeGeneric))
            {
                var filterType = attr.AttributeClass!.TypeArguments[0];
                interceptorMetas.Add(new InterceptorMeta(attr, (INamedTypeSymbol)filterType));
            }
        }
        InterceptorMetas = interceptorMetas.ToArray();

        CommandFullTypeName = CommandTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        CommandTypePrefix = CommandFullTypeName
            .Replace("global::", "")
            .Replace(".", "")
            .Replace("<", "_")
            .Replace(">", "_");
        CommandTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        TakeCancellationToken = cancellationTokenParamSymbol is not null;
        TakePublishContext = publishContextParamSymbol is not null;

        this.references = references;
    }

    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        return Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
    }
}