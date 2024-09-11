using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VitalRouter.SourceGenerator.MRuby;

class MRubyCommandMeta(string key, INamedTypeSymbol type, SyntaxNode syntax)
{
    public string Key => key;
    public INamedTypeSymbol CommandType => type;
    public SyntaxNode Syntax => syntax;

    public bool IsMessagePackObjectStringKey(ReferenceSymbols referenceSymbols)
    {
        var attr = CommandType.GetAttribute(referenceSymbols.MessagePackObjectAttribute);
        if (attr is null) return false;

        return (bool?)attr.ConstructorArguments[0].Value == true;
    }
}

class TypeMeta
{
    public TypeDeclarationSyntax Syntax { get; }
    public INamedTypeSymbol Symbol { get; }
    public IReadOnlyList<MRubyCommandMeta> CommandMetas { get; }
    public string TypeName { get; }
    public string FullTypeName { get; }

    public TypeMeta(
        TypeDeclarationSyntax syntax,
        INamedTypeSymbol symbol,
        ImmutableArray<AttributeData> attributes)
    {
        Syntax = syntax;
        Symbol = symbol;

        TypeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        CommandMetas = attributes.Select(attributeData =>
        {
            var key = attributeData.ConstructorArguments[0].Value as string;
            var type = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
            var s = attributeData.ApplicationSyntaxReference!.GetSyntax();
            return new MRubyCommandMeta(key!, type!, s);
        }).ToArray();
    }

    public bool IsPartial()
    {
        return Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    public bool IsNested()
    {
        return Syntax.Parent is TypeDeclarationSyntax;
    }
}