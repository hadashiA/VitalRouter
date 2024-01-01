using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VitalRouter.SourceGenerator;

class TypeMeta
{
    public TypeDeclarationSyntax Syntax { get; }
    public INamedTypeSymbol Symbol { get; }
    public AttributeData RoutingAttribute { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }

    public InterceptorMeta[] DefaultInterceptorMetas { get; }
    public IReadOnlyList<RouteMethodMeta> SyncRouteMethodMetas => syncRouteMethodMetas;
    public IReadOnlyList<RouteMethodMeta> AsyncRouteMethodMetas => asyncRouteMethodMetas;
    public IReadOnlyList<RouteMethodMeta> InterceptRouteMethodMetas => interceptRouteMethodMetas;
    public IReadOnlyList<IMethodSymbol> NonRoutableMethodSymbols => nonRoutableMethodSymbols;

    readonly ReferenceSymbols references;
    readonly List<RouteMethodMeta> syncRouteMethodMetas = [];
    readonly List<RouteMethodMeta> asyncRouteMethodMetas = [];
    readonly List<RouteMethodMeta> interceptRouteMethodMetas = [];
    readonly List<IMethodSymbol> nonRoutableMethodSymbols = [];

    public TypeMeta(
        TypeDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        AttributeData routingAttribute,
        ReferenceSymbols references)
    {
        Syntax = syntax;
        Symbol = symbol;
        this.references = references;

        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        RoutingAttribute = routingAttribute;

        DefaultInterceptorMetas = symbol.GetAttributes()
            .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, references.FilterAttribute) &&
                        x.ConstructorArguments is [{ Kind: TypedConstantKind.Type }, ..])
            .Select(x => new InterceptorMeta((INamedTypeSymbol)x.ConstructorArguments[0].Value!))
            .ToArray();

        CollectMembers();
    }

    public bool IsPartial()
    {
        return Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    public bool IsNested()
    {
        return Syntax.Parent is TypeDeclarationSyntax;
    }

    public IEnumerable<RouteMethodMeta> AllRouteMethodMetas()
    {
        return SyncRouteMethodMetas
            .Concat(AsyncRouteMethodMetas)
            .Concat(InterceptRouteMethodMetas);
    }

    void CollectMembers()
    {
        var i = 0;
        foreach (var member in Symbol.GetAllMembers())
        {
            if (member is IMethodSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } method)
            {
                if (method.Parameters.Length is <= 0 or >= 3)
                    continue;

                var commandParam = method.Parameters[0];
                if (!commandParam.Type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, references.CommandInterface)))
                {
                    continue;
                }

                // sync
                if (method is { ReturnsVoid: true, Parameters.Length: 1 })
                {
                    var methodMeta = new RouteMethodMeta(method, commandParam.Type, references, i++);
                    if (DefaultInterceptorMetas.Length > 0 || methodMeta.InterceptorMetas.Length > 0)
                    {
                        interceptRouteMethodMetas.Add(methodMeta);
                    }
                    else
                    {
                        syncRouteMethodMetas.Add(methodMeta);
                    }
                }
                // async
                else if (SymbolEqualityComparer.Default.Equals(method.ReturnType, references.UniTaskType) ||
                         SymbolEqualityComparer.Default.Equals(method.ReturnType, references.AwaitableType) ||
                         SymbolEqualityComparer.Default.Equals(method.ReturnType, references.TaskType) ||
                         SymbolEqualityComparer.Default.Equals(method.ReturnType, references.ValueTaskType))
                {
                    var methodMeta = new RouteMethodMeta(method, commandParam.Type, references, i++);
                    if (DefaultInterceptorMetas.Length > 0 || methodMeta.InterceptorMetas.Length > 0)
                    {
                        interceptRouteMethodMetas.Add(methodMeta);
                    }
                    else
                    {
                        asyncRouteMethodMetas.Add(methodMeta);
                    }
                }
                // not routable
                else
                {
                    if (SymbolEqualityComparer.Default.Equals(method.ContainingType, Symbol))
                    {
                        nonRoutableMethodSymbols.Add(method);
                    }
                }
            }
        }
    }
}
