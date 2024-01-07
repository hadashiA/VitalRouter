using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using GeneratorAttributeSyntaxContext = Microsoft.CodeAnalysis.DotnetRuntime.Extensions.GeneratorAttributeSyntaxContext;

namespace VitalRouter.SourceGenerator;

[Generator]
public class VitalRouterIncrementalSourceGenerator : IIncrementalGenerator
{
    class Comparer : IEqualityComparer<(GeneratorAttributeSyntaxContext, Compilation)>
    {
        public static readonly Comparer Instance = new();

        public bool Equals((GeneratorAttributeSyntaxContext, Compilation) x, (GeneratorAttributeSyntaxContext, Compilation) y)
        {
            return x.Item1.TargetNode.Equals(y.Item1.TargetNode);
        }

        public int GetHashCode((GeneratorAttributeSyntaxContext, Compilation) obj)
        {
            return obj.Item1.TargetNode.GetHashCode();
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                context,
                "VitalRouter.RoutesAttribute",
                static (node, cancellation) => node is ClassDeclarationSyntax,
                static (context, cancellation) => context)
            .Combine(context.CompilationProvider)
            .WithComparer(Comparer.Instance);

        // Generate the source code.
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (sourceProductionContext, t) =>
            {
                var (compilation, list) = t;
                var references = ReferenceSymbols.Create(compilation);
                if (references is null)
                {
                    return;
                }

                var stringBuilder = new StringBuilder();

                foreach (var (x, _) in list)
                {
                    var typeMeta = new TypeMeta(
                        (TypeDeclarationSyntax)x.TargetNode,
                        (INamedTypeSymbol)x.TargetSymbol,
                        x.Attributes.First(),
                        references);

                    if (TryEmit(typeMeta, references, stringBuilder, sourceProductionContext))
                    {
                        var fullType = typeMeta.FullTypeName
                            .Replace("global::", "")
                            .Replace("<", "_")
                            .Replace(">", "_");

                        sourceProductionContext.AddSource($"{fullType}.g.cs", stringBuilder.ToString());
                    }
                    stringBuilder.Clear();
                }
            });
    }

    static bool TryEmit(TypeMeta typeMeta, ReferenceSymbols references, StringBuilder builder, in SourceProductionContext context)
    {
        try
        {
            var error = false;

            // verify is partial
            if (!typeMeta.IsPartial())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MustBePartial,
                    typeMeta.Syntax.Identifier.GetLocation(),
                    typeMeta.Symbol.Name));
                error = true;
            }

            // nested is not allowed
            if (typeMeta.IsNested())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NestedNotAllow,
                    typeMeta.Syntax.Identifier.GetLocation(),
                    typeMeta.Symbol.Name));
                error = true;
            }

            // check duplicates of the command argument
            foreach (var methodMeta in typeMeta.RouteMethodMetas)
            {
                if (typeMeta.RouteMethodMetas.Any(x => x != methodMeta && SymbolEqualityComparer.Default.Equals(x.CommandTypeSymbol, methodMeta.CommandTypeSymbol)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateRouteMethodDefined,
                        methodMeta.Symbol.Locations.FirstOrDefault() ?? typeMeta.Syntax.GetLocation(),
                        methodMeta.Symbol.Name));
                    error = true;
                }
            }

            // check interceptor type
            foreach (var interceptorMeta in typeMeta.AllInterceptorMetas)
            {
                if (!interceptorMeta.TypeSymbol.Interfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, references.InterceptorInterface)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidInterceptorType,
                        interceptorMeta.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeMeta.Syntax.GetLocation(),
                        interceptorMeta.TypeSymbol.Name));
                    error = true;
                }
            }

            // check redundant
            foreach (var interceptorMeta in typeMeta.DefaultInterceptorMetas)
            {
                var redundant = typeMeta.AllInterceptorMetas
                    .Where(x => interceptorMeta != x &&
                                SymbolEqualityComparer.Default.Equals(interceptorMeta.TypeSymbol, x.TypeSymbol));
                foreach (var x in redundant)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RedundantInterceptorType,
                        x.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeMeta.Syntax.GetLocation(),
                        x.TypeSymbol.Name));
                    error = true;
                }
            }
            foreach (var method in typeMeta.RouteMethodMetas)
            {
                foreach (var interceptorMeta in method.InterceptorMetas)
                {
                    var redundant = typeMeta.DefaultInterceptorMetas
                        .Where(x => SymbolEqualityComparer.Default.Equals(interceptorMeta.TypeSymbol, x.TypeSymbol));
                    foreach (var x in redundant)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.RedundantInterceptorType,
                            x.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeMeta.Syntax.GetLocation(),
                            x.TypeSymbol.Name));
                        error = true;
                    }
                }
            }


            if (error)
            {
                return false;
            }

            // foreach (var nonRoutableMethod in typeMeta.NonRoutableMethodSymbols)
            // {
            //     context.ReportDiagnostic(Diagnostic.Create(
            //         DiagnosticDescriptors.NoRoutablePublicMethodDefined,
            //         nonRoutableMethod.Locations.FirstOrDefault() ?? typeMeta.Syntax.GetLocation(),
            //         nonRoutableMethod.Name));
            // }

            if (typeMeta.RouteMethodMetas.Count <= 0)
            {
                return false;
            }

            builder.AppendLine("""
// <auto-generated />
#nullable enable
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602 // Possible null return
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VitalRouter;

""");

            var ns = typeMeta.Symbol.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                builder.AppendLine($$"""
namespace {{ns}} 
{
""");
            }

            builder.AppendLine($$"""
partial class {{typeMeta.TypeName}}
{
""");
            if (!TryEmitMappingMethod(typeMeta, references, builder))
            {
                return false;
            }
            if (!TryEmitSubscriber(typeMeta, builder))
            {
                return false;
            }
            if (!TryEmitAsyncSubscriber(typeMeta, builder))
            {
                return false;
            }
            if (!TryEmitAsyncSubscriberCore(typeMeta, builder))
            {
                return false;
            }

            builder.AppendLine("""
}
""");
            if (!ns.IsGlobalNamespace)
            {
                builder.AppendLine("}");
            }

            builder.AppendLine($$"""
#pragma warning restore CS0162 // Unreachable code
#pragma warning restore CS0219 // Variable assigned but never used
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8601 // Possible null reference assignment
#pragma warning restore CS8602 // Possible null return
#pragma warning restore CS8604 // Possible null reference argument for parameter
#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method
""");
            return true;
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnexpectedErrorDescriptor,
                Location.None,
                ex.ToString()));
            return false;
        }
    }

    static bool TryEmitMappingMethod(TypeMeta typeMeta, ReferenceSymbols references, StringBuilder builder)
    {
        var parameters = new[] { "ICommandSubscribable subscribable" }
            .Concat(typeMeta.AllInterceptorMetas.Select(x => $"{x.FullTypeName} {x.VariableName}"))
            .Distinct();

        builder.AppendLine($$"""
    Subscription vitalRouterGeneratedSubscription;

    [global::VitalRouter.Preserve]
    public void MapTo({{string.Join(", ", parameters)}})
    {
        UnmapRoutes();

""");
        var hasSubscriber = typeMeta.DefaultInterceptorMetas.Length <= 0 &&
                            typeMeta.RouteMethodMetas.Any(x => !x.IsAsync && x.InterceptorMetas.Length <= 0);
        var hasAsyncSubscriber = typeMeta.DefaultInterceptorMetas.Length > 0 ||
                                 typeMeta.RouteMethodMetas.Any(x => x.IsAsync || x.InterceptorMetas.Length > 0);
        if (hasSubscriber)
        {
            builder.AppendLine($$"""
        var subscriber = new VitalRouterGeneratedSubscriber(this);
        subscribable.Subscribe(subscriber);
""");
        }
        if (hasAsyncSubscriber)
        {
            var asyncSubscriberArgs = new[] { "this" }
                .Concat(typeMeta.AllInterceptorMetas.Select(x => $"{x.VariableName}"))
                .Distinct();

            builder.AppendLine($$"""
        var asyncSubscriber  = new VitalRouterGeneratedAsyncSubscriber({{string.Join(", ", asyncSubscriberArgs)}});
        subscribable.Subscribe(asyncSubscriber);
""");
        }

        var subscriptionArgs = (hasSubscriber, hasAsyncSubscriber) switch
        {
            (true, true) => "subscriber, asyncSubscriber",
            (true, false) => "subscriber",
            (false, true) => "asyncSubscriber",
            _ => throw new InvalidOperationException()
        };

        builder.AppendLine($$"""
        vitalRouterGeneratedSubscription = new Subscription(subscribable, {{subscriptionArgs}});
""");

        if (typeMeta.Symbol.InheritsFrom(references.MonoBehaviourType))
        {
            builder.AppendLine($$"""
        
        if (!gameObject.TryGetComponent(typeof(VitalRouter.Unity.SubscriptionHandle), out var handle))
        {
            handle = gameObject.AddComponent<VitalRouter.Unity.SubscriptionHandle>();
        }
        ((VitalRouter.Unity.SubscriptionHandle)handle).Subscription = vitalRouterGeneratedSubscription;
""");
        }
        builder.AppendLine($$"""
    }

    public void UnmapRoutes()
    {
        vitalRouterGeneratedSubscription.Dispose();
    }

""");
        return true;
    }

    static bool TryEmitSubscriber(TypeMeta typeMeta, StringBuilder builder)
    {
        if (typeMeta.DefaultInterceptorMetas.Length > 0)
        {
            return true;
        }

        var methods = typeMeta.RouteMethodMetas.Where(x => x is { IsAsync: false, InterceptorMetas.Length: <= 0 });
        if (!methods.Any())
        {
            return true;
        }

        builder.AppendLine($$"""
    class VitalRouterGeneratedSubscriber : ICommandSubscriber
    {
        readonly {{typeMeta.TypeName}} source;
    
        public VitalRouterGeneratedSubscriber({{typeMeta.TypeName}} source)
        {
            this.source = source;
        }
    
        public void Receive<T>(T command) where T : ICommand
        {
            switch (command)
            {
""");
        foreach (var methodMeta in methods)
        {
            builder.AppendLine($$"""
                case {{methodMeta.CommandFullTypeName}} x:
                    source.{{methodMeta.Symbol.Name}}(x);
                    break;
""");
        }
        builder.AppendLine($$"""
                default:
                    break;
            }
        }
    }

""");
        return true;
    }

    static bool TryEmitAsyncSubscriber(TypeMeta typeMeta, StringBuilder builder)
    {
        var methods = typeMeta.DefaultInterceptorMetas.Length > 0
            ? typeMeta.RouteMethodMetas
            : typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0 || x.IsAsync);

        if (!methods.Any())
        {
            return true;
        }

        var interceptorParams = typeMeta.AllInterceptorMetas
            .Select(x => $"{x.FullTypeName} {x.VariableName}")
            .Distinct();
        var constructorParams = new[] { $"{typeMeta.TypeName} source" }.Concat(interceptorParams);

         builder.AppendLine($$"""
    class VitalRouterGeneratedAsyncSubscriber : IAsyncCommandSubscriber
    {
         readonly {{typeMeta.TypeName}} source;
""");
         if (typeMeta.AllInterceptorMetas.Count > 0)
         {
             builder.AppendLine($"""
        readonly VitalRouterGeneratedAsyncSubscriberCore core;
""");
         }
         if (typeMeta.DefaultInterceptorMetas.Length > 0)
         {
             builder.AppendLine($$"""
         readonly ICommandInterceptor[] interceptorStackDefault;
""");
         }
         foreach (var method in typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0))
         {
             if (method.InterceptorMetas.Length > 0)
             {
                 builder.AppendLine($$"""
         readonly ICommandInterceptor[] interceptorStack{{method.CommandTypePrefix}};
""");
             }
         }

        builder.AppendLine($$"""

        public VitalRouterGeneratedAsyncSubscriber({{string.Join(", ", constructorParams)}})
        {
            this.source = source;
""");
        if (typeMeta.AllInterceptorMetas.Count > 0)
        {
            builder.AppendLine($$"""
            this.core = new VitalRouterGeneratedAsyncSubscriberCore(source);
""");
        }

        if (typeMeta.DefaultInterceptorMetas.Length > 0)
        {
            builder.AppendLine($$"""
            interceptorStackDefault = new ICommandInterceptor[] { {{string.Join(", ", typeMeta.DefaultInterceptorMetas.Select(x => x.VariableName))}}, core };
""");
        }
        foreach (var method in typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0))
        {
            if (method.InterceptorMetas.Length > 0)
            {
                builder.AppendLine($$"""
            interceptorStack{{method.CommandTypePrefix}} = new ICommandInterceptor[] { {{string.Join(", ", typeMeta.DefaultInterceptorMetas.Concat(method.InterceptorMetas).Select(x => x.VariableName))}}, core };
""");
            }
        }

        builder.AppendLine($$"""
        }
        
        public UniTask ReceiveAsync<T>(T command, CancellationToken cancellation) where T : ICommand
        {
            switch (command)
            {
""");
        foreach (var method in methods)
        {
            if (typeMeta.DefaultInterceptorMetas.Length > 0 || method.InterceptorMetas.Length > 0)
            {
                var interceptorStackName = method.InterceptorMetas.Length > 0 ? $"interceptorStack{method.CommandTypePrefix}" : "interceptorStackDefault";
                builder.AppendLine($$"""
                case {{method.CommandFullTypeName}} x:
                {
                    var context = InvokeContext<T>.Rent({{interceptorStackName}});
                    try
                    {
                        return context.InvokeRecursiveAsync(command, cancellation);
                    }
                    finally
                    {
                        context.Return();
                    }
                }
""");
            }
            else if (method.IsAsync)
            {
                if (method.IsAsync)
                {
                    if (method.TakeCancellationToken)
                    {
                        builder.AppendLine($$"""
                case {{method.CommandFullTypeName}} x:
                    return source.{{method.Symbol.Name}}(x, cancellation);
""");
                    }
                    else
                    {
                        builder.AppendLine($$"""
                case {{method.CommandFullTypeName}} x:
                    return source.{{method.Symbol.Name}}(x);
""");
                    }
                }
                else
                {
                    builder.AppendLine($$"""
                case {{method.CommandFullTypeName}} x:
                    source.{{method.Symbol.Name}}(x);
                    return UniTask.CompletedTask;
""");
                }
            }
        }
        builder.AppendLine($$"""
                default:
                    return UniTask.CompletedTask;
            }
        }
    }

""");
        return true;
    }

    static bool TryEmitAsyncSubscriberCore(TypeMeta typeMeta, StringBuilder builder)
    {
        var methods = typeMeta.DefaultInterceptorMetas.Length > 0
            ? typeMeta.RouteMethodMetas
            : typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0);

        if (!methods.Any())
        {
            return true;
        }

        builder.AppendLine($$"""
    class VitalRouterGeneratedAsyncSubscriberCore : ICommandInterceptor
    {
        readonly {{typeMeta.TypeName}} source;
    
        public VitalRouterGeneratedAsyncSubscriberCore({{typeMeta.TypeName}} source)
        {
            this.source = source;
        }
    
        public UniTask InvokeAsync<T>(
            T command, 
            CancellationToken cancellation,
            Func<T, CancellationToken, UniTask> _) where T : ICommand
        {
            switch (command)
            {
""");
        foreach (var methodMeta in methods)
        {
            if (methodMeta.IsAsync)
            {
                if (methodMeta.TakeCancellationToken)
                {
                    builder.AppendLine($$"""
                case {{methodMeta.CommandFullTypeName}} x:
                    return source.{{methodMeta.Symbol.Name}}(x, cancellation);
""");
                }
                else
                {
                    builder.AppendLine($$"""
                case {{methodMeta.CommandFullTypeName}} x:
                    return source.{{methodMeta.Symbol.Name}}(x);
""");
                }
            }
            else
            {
                    builder.AppendLine($$"""
                case {{methodMeta.CommandFullTypeName}} x:
                    source.{{methodMeta.Symbol.Name}}(x);
                    return UniTask.CompletedTask;
""");
            }
        }
        builder.AppendLine($$"""
                default:
                    return UniTask.CompletedTask;
            }
        }
    }

""");
        return true;
    }
}
