using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var routingAttribute = compilation.GetTypeByMetadataName("VitalRouter.RoutesAttribute");
        if (routingAttribute is null)
            return null;

        return new ReferenceSymbols
        {
            RoutingAttribute = routingAttribute,
            FilterAttribute = compilation.GetTypeByMetadataName("VitalRouter.FilterAttribute")!,
            CommandInterface = compilation.GetTypeByMetadataName("VitalRouter.ICommand")!,
            InterceptorInterface = compilation.GetTypeByMetadataName("VitalRouter.ICommandInterceptor")!,
            MonoBehaviourType = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour")!,
            CancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken")!,
            UniTaskType = compilation.GetTypeByMetadataName("Cysharp.Threading.Tasks.UniTask"),
            AwaitableType = compilation.GetTypeByMetadataName("UnityEngine.Awaitable"),
            TaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task"),
            ValueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask"),
        };
    }

    public INamedTypeSymbol RoutingAttribute { get; private set; } = default!;
    public INamedTypeSymbol FilterAttribute { get; private set; } = default!;
    public INamedTypeSymbol CommandInterface { get; private set; } = default!;
    public INamedTypeSymbol InterceptorInterface { get; private set; } = default!;
    public INamedTypeSymbol MonoBehaviourType { get; private set; } = default!;
    public INamedTypeSymbol CancellationTokenType { get; private set; } = default!;
    public INamedTypeSymbol? UniTaskType { get; private set; } = default!;
    public INamedTypeSymbol? AwaitableType { get; private set; } = default!;
    public INamedTypeSymbol? TaskType { get; private set; } = default!;
    public INamedTypeSymbol? ValueTaskType { get; private set; } = default!;
}