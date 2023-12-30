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

    public IReadOnlyList<MemberMeta> MemberMetas => memberMetas ??= GetRuoteMembers();

    ReferenceSymbols references;
    MemberMeta[]? memberMetas;

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
    }

    public bool IsPartial()
    {
        return Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    public bool IsNested()
    {
        return Syntax.Parent is TypeDeclarationSyntax;
    }

    MemberMeta[] GetRuotesMembers()
    {
        if (memberMetas == null)
        {
            memberMetas = Symbol.GetMembers() // iterate includes parent type
                .Where(x =>
                {
                    if (x is IMethodSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } methodSymbol)
                    {
                        return true;
                    }
                    return false;
                })
                .Select((x, i) => new MemberMeta((IMethodSymbol)x, references, i))
                .ToArray();
        }
        return memberMetas;
    }
}
