using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace VitalRouter.SourceGenerator;

[Generator]
public class VitalRouterIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                context,
                "VitalRouter.RoutesAttribute",
                static (node, cancellation) => node is ClassDeclarationSyntax,
                static (context, cancellation) => context)
            .Combine(context.CompilationProvider);

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
                if (typeMeta.RouteMethodMetas.Any(x => x != methodMeta &&
                                                       SymbolEqualityComparer.Default.Equals(x.CommandTypeSymbol, methodMeta.CommandTypeSymbol)))
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VitalRouter;
using VitalRouter.Internal;

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
    readonly List<Subscription> vitalRouterGeneratedSubscriptions = new List<Subscription>();

    [global::VitalRouter.Preserve]
    public Subscription MapTo({{string.Join(", ", parameters)}})
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
        var subscription = new Subscription(subscribable, {{subscriptionArgs}});
""");

        if (references.MonoBehaviourType != null && typeMeta.Symbol.InheritsFrom(references.MonoBehaviourType))
        {
            builder.AppendLine($$"""
        
        if (!gameObject.TryGetComponent(typeof(VitalRouter.Unity.SubscriptionHandle), out var handle))
        {
            handle = gameObject.AddComponent<VitalRouter.Unity.SubscriptionHandle>();
        }
        ((VitalRouter.Unity.SubscriptionHandle)handle).Subscriptions.Add(subscription);
""");
        }
        builder.AppendLine($$"""
        lock (vitalRouterGeneratedSubscriptions)
        {
            vitalRouterGeneratedSubscriptions.Add(subscription);
        }
        return subscription; 
    }

    [global::VitalRouter.Preserve]
    public void UnmapRoutes()
    {
        lock (vitalRouterGeneratedSubscriptions)
        {
            foreach (var subscription in vitalRouterGeneratedSubscriptions)
            {
                subscription.Dispose();
            }
            vitalRouterGeneratedSubscriptions.Clear();
        }        
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

        var methods = typeMeta.RouteMethodMetas
            .Where(x => x is { IsAsync: false, InterceptorMetas.Length: <= 0 })
            .ToArray();

        if (!methods.Any())
        {
            return true;
        }

        builder.AppendLine($$"""
    class VitalRouterGeneratedSubscriber : ICommandSubscriber
    {
        static class MethodTable<T> where T : ICommand
        {
            public static Action<{{typeMeta.TypeName}}, T, PublishContext>? Value;
        }
        
        static VitalRouterGeneratedSubscriber()
        {
""");
        foreach (var method in methods)
        {
            if (method.TakePublishContext)
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.Value = static (source, command, ctx) => source.{{method.Symbol.Name}}(command, ctx);
""");
            }
            else if (method.TakeCancellationToken)
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.Value = static (source, command, ctx) => source.{{method.Symbol.Name}}(command, ctx.CancellationToken);
""");
            }
            else
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.Value = static (source, command, ctx) => source.{{method.Symbol.Name}}(command);
""");
            }
        }

        builder.AppendLine($$"""
        }
        
        readonly {{typeMeta.TypeName}} source;
    
        public VitalRouterGeneratedSubscriber({{typeMeta.TypeName}} source)
        {
            this.source = source;
        }

        public void Receive<T>(T command, PublishContext context) where T : ICommand
        {
            MethodTable<T>.Value?.Invoke(source, command, context);
        }
    }

""");
        return true;
    }

    static bool TryEmitAsyncSubscriber(TypeMeta typeMeta, StringBuilder builder)
    {
        var methods = typeMeta.DefaultInterceptorMetas.Length > 0
            ? typeMeta.RouteMethodMetas
            : typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0 || x.IsAsync).ToArray();

        if (!methods.Any())
        {
            return true;
        }

        var interceptorParams = typeMeta.AllInterceptorMetas.Select(x => $"{x.FullTypeName} {x.VariableName}");
        var constructorParams = new[] { $"{typeMeta.TypeName} source" }.Concat(interceptorParams);

         builder.AppendLine($$"""
    class VitalRouterGeneratedAsyncSubscriber : IAsyncCommandSubscriber
    {
        static class MethodTable<T> where T : ICommand
        {
            public static Func<{{typeMeta.FullTypeName}}, T, PublishContext, ValueTask>? Value;
            public static Func<VitalRouterGeneratedAsyncSubscriber, ICommandInterceptor[]>? InterceptorFinder;
        }
        
        static VitalRouterGeneratedAsyncSubscriber()
        {
""");
        foreach (var method in methods)
        {
            if (method.InterceptorMetas.Length > 0)
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.InterceptorFinder = static self => self.interceptorStack{{method.CommandTypePrefix}};
""");
            }
            else if (typeMeta.DefaultInterceptorMetas.Length > 0)
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.InterceptorFinder = static self => self.interceptorStackDefault;
""");
            }
            else
            {
                builder.AppendLine($$"""
            MethodTable<{{method.CommandFullTypeName}}>.Value = {{GetMethodTableEntry(method)}};
""");
            }
        }
        builder.AppendLine($$"""
        }
    
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
            builder.AppendLine("""
            this.core = new VitalRouterGeneratedAsyncSubscriberCore(source);
""");
        }

        if (typeMeta.DefaultInterceptorMetas.Length > 0)
        {
            builder.AppendLine($$"""
            interceptorStackDefault = new ICommandInterceptor[] { {{string.Join(", ", typeMeta.DefaultInterceptorMetas.Select(x => x.VariableName))}}, core };
""");
        }
        foreach (var method in typeMeta.RouteMethodMetas)
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
        
        public ValueTask ReceiveAsync<T>(T command, PublishContext context) where T : ICommand
        {
            if (MethodTable<T>.Value is { } method)
            {
                return method.Invoke(source, command, context);
            }
            if (MethodTable<T>.InterceptorFinder is { } finder)
            {
                var interceptorStack = finder.Invoke(this);
                return ReceiveContext<T>.InvokeAsync(command, interceptorStack, context);
            }
            return default;
        }
    }
""");
        return true;
    }

    static bool TryEmitAsyncSubscriberCore(TypeMeta typeMeta, StringBuilder builder)
    {
        var methods = typeMeta.DefaultInterceptorMetas.Length > 0
            ? typeMeta.RouteMethodMetas
            : typeMeta.RouteMethodMetas.Where(x => x.InterceptorMetas.Length > 0).ToArray();

        if (methods.Count <= 0)
        {
            return true;
        }

        builder.AppendLine($$"""
    class VitalRouterGeneratedAsyncSubscriberCore : ICommandInterceptor
    {
        static class MethodTable<T> where T : ICommand
        {
            public static Func<{{typeMeta.TypeName}}, T, PublishContext, ValueTask>? Value;
        }
        
        static VitalRouterGeneratedAsyncSubscriberCore()
        {
""");
        foreach (var methodMeta in methods)
        {
                builder.AppendLine($$"""
            MethodTable<{{methodMeta.CommandFullTypeName}}>.Value = {{GetMethodTableEntry(methodMeta)}};
""");
        }

        builder.AppendLine($$"""
        }
        
        readonly {{typeMeta.TypeName}} source;
    
        public VitalRouterGeneratedAsyncSubscriberCore({{typeMeta.TypeName}} source)
        {
            this.source = source;
        }
    
        public ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> _) where T : ICommand
        {
            return MethodTable<T>.Value?.Invoke(source, command, context) ?? default;
        }
    }
""");
        return true;
    }

    static string GetMethodTableEntry(RouteMethodMeta method)
    {
        if (method.IsAsync)
        {
            if (method.TakePublishContext)
            {
                if (method.IsValueTask())
                {
                    return $"static (source, command, context) => (ValueTask)source.{method.Symbol.Name}(command, context)";
                }
                return $"static async (source, command, context) => await source.{method.Symbol.Name}(command, context)";
            }
            if (method.TakeCancellationToken)
            {
                if (method.IsValueTask() || method.IsUniTask())
                {
                    return $"static (source, command, context) => (ValueTask)source.{method.Symbol.Name}(command, context.CancellationToken)";
                }
                return $"static async (source, command, context) => await source.{method.Symbol.Name}(command, context.CancellationToken)";
            }
            if (method.IsValueTask() || method.IsUniTask())
            {
                return $"static (source, command, context) => (ValueTask)source.{method.Symbol.Name}(command)";
            }
            return $"static async (source, command, context) => await source.{method.Symbol.Name}(command)";
        }

        if (method.TakePublishContext)
        {
            return $"static (source, command, context) => {{ source.{method.Symbol.Name}(command, context); return default; }}";
        }

        if (method.TakeCancellationToken)
        {
            return $"static (source, command, context) => {{ source.{method.Symbol.Name}(command, context.CancellationToken); return default; }}";
        }
        return $"static (source, command, context) => {{ source.{method.Symbol.Name}(command); return default; }}";
    }
}
