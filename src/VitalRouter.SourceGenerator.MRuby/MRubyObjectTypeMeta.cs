using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AttributeData = Microsoft.CodeAnalysis.AttributeData;
using IMethodSymbol = Microsoft.CodeAnalysis.IMethodSymbol;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace VitalRouter.SourceGenerator.MRuby;

class MRubyObjectTypeMeta
{
    public TypeDeclarationSyntax Syntax { get; }
    public INamedTypeSymbol Symbol { get; }
    public AttributeData MRubyObjectAttribute { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }
    public IReadOnlyList<IMethodSymbol> Constructors { get; }
    public IReadOnlyList<MRubyObjectMemberMeta> MemberMetas => memberMetas ??= GetSerializeMembers();

    ReferenceSymbols references;
    MRubyObjectMemberMeta[]? memberMetas;

    public MRubyObjectTypeMeta(
        TypeDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        AttributeData mrubyObjectAttribute,
        ReferenceSymbols references)
    {
        Syntax = syntax;
        Symbol = symbol;
        this.references = references;

        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        MRubyObjectAttribute = mrubyObjectAttribute;

        Constructors = symbol.InstanceConstructors
            .Where(x => !x.IsImplicitlyDeclared) // remove empty ctor(struct always generate it), record's clone ctor
            .ToArray();
    }

    public bool IsPartial()
    {
        return Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    public bool IsNested()
    {
        return Syntax.Parent is TypeDeclarationSyntax;
    }

    MRubyObjectMemberMeta[] GetSerializeMembers()
    {
        if (memberMetas == null)
        {
            memberMetas = Symbol.GetAllMembers() // iterate includes parent type
                .Where(x => x is (IFieldSymbol or IPropertySymbol) and { IsStatic: false, IsImplicitlyDeclared: false })
                .Where(x =>
                {
                    if (x.ContainsAttribute(references.MRubyIgnoreAttribute)) return false;
                    if (x.DeclaredAccessibility != Accessibility.Public) return false;

                    if (x is IPropertySymbol p)
                    {
                        // set only can't be serializable member
                        if (p.GetMethod == null && p.SetMethod != null)
                        {
                            return false;
                        }
                        if (p.IsIndexer) return false;
                    }
                    return true;
                })
                .Select((x, i) => new MRubyObjectMemberMeta(x, references))
                .ToArray();
        }
        return memberMetas;
    }
}
