using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var routingAttribute = compilation.GetTypeByMetadataName("VitalRouter.RoutingAttribute");
        if (routingAttribute is null)
            return null;

        return new ReferenceSymbols
        {
            RoutingAttribute = routingAttribute,
            FilterAttribute = compilation.GetTypeByMetadataName("VitalRouter.FilterAttribute")!,
            CommandInterface = compilation.GetTypeByMetadataName("VitalRouter.ICommand")!,
            UniTaskType = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask"),
        };
    }

    public INamedTypeSymbol RoutingAttribute { get; private set; } = default!;
    public INamedTypeSymbol FilterAttribute { get; private set; } = default!;
    public INamedTypeSymbol CommandInterface { get; private set; } = default!;
    public INamedTypeSymbol? UniTaskType { get; private set; } = default!;
}