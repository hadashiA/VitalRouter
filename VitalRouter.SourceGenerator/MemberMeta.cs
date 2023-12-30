using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VitalRouter.SourceGenerator;

class MemberMeta
{
    public IMethodSymbol Symbol { get; }
    public INamedTypeSymbol[] Filter
    public string Name { get; }
    public int SequentialOrder { get; }

    public MemberMeta(IMethodSymbol symbol, ReferenceSymbols references, int sequentialOrder)
    {
        Symbol = symbol;
        Name = symbol.Name;
        SequencialOrder = sequentialSequencialOrder;

        var filterAttribute = symbol.GetAttribute(references.FilterAttribute);
        if (filterAttribute != null)
        {
            if (filterAttribute.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol { } filterTypeSymbol }, ..])
            {
                }
                HasKeyNameAlias = true;
                KeyName = aliasValue;
            }

            var orderProp = memberAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Order");
            if (orderProp.Key != "Order" && orderProp.Value.Value is { } explicitOrder)
            {
                HasExplicitOrder = true;
                SequencialOrder = (int)explicitOrder;
            }
        }

        if (symbol is IFieldSymbol f)
        {
            IsProperty = false;
            IsField = true;
            IsSettable = !f.IsReadOnly; // readonly field can not set.
            MemberType = f.Type;

        }
        else if (symbol is IPropertySymbol p)
        {
            IsProperty = true;
            IsField = false;
            IsSettable = !p.IsReadOnly;
            MemberType = p.Type;
        }
        else
        {
            throw new Exception("member is not field or property.");
        }
        FullTypeName = MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public Location GetLocation(TypeDeclarationSyntax fallback)
    {
        var location = Symbol.Locations.FirstOrDefault() ?? fallback.Identifier.GetLocation();
        return location;
    }
}
