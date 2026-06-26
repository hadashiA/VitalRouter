using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace VitalRouter.SourceGenerator;

/// <summary>
/// Fully value-equatable description of one interceptor. No symbols.
/// </summary>
sealed record InterceptorModel(string FullTypeName, string VariableName);

/// <summary>
/// Fully value-equatable description of one [Route] method's signature. No symbols,
/// no syntax — so a method body edit that leaves the signature intact produces an
/// equal model and the source output is skipped.
/// </summary>
sealed record RouteModel(
    string MethodName,
    string CommandFullTypeName,
    string CommandTypePrefix,
    bool IsAsync,
    bool IsValueTask,
    bool IsUniTask,
    bool TakeCancellationToken,
    bool TakePublishContext,
    EquatableArray<InterceptorModel> Interceptors);

/// <summary>
/// Fully value-equatable description of one [Routes] type. This is the unit of
/// incremental caching: equal model in -> source output skipped.
/// </summary>
sealed record TypeModel(
    string TypeName,
    string FullTypeName,
    string? Namespace,
    string HintName,
    bool InheritsMonoBehaviour,
    bool HasError,
    EquatableArray<RouteModel> Routes,
    EquatableArray<InterceptorModel> DefaultInterceptors,
    EquatableArray<InterceptorModel> AllInterceptors,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// Converts the symbol/syntax world (which must not enter the model) into a cacheable
/// <see cref="TypeModel"/>. All validation that needs semantic info happens here, and
/// is reduced to <see cref="DiagnosticInfo"/> entries.
/// </summary>
static class Parser
{
    public static TypeModel Parse(GeneratorAttributeSyntaxContext context, ReferenceSymbols references)
    {
        var typeMeta = new TypeMeta(
            (TypeDeclarationSyntax)context.TargetNode,
            (INamedTypeSymbol)context.TargetSymbol,
            context.Attributes.First(),
            references);

        var diagnostics = new List<DiagnosticInfo>();
        var error = false;

        // verify is partial
        if (!typeMeta.IsPartial())
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.MustBePartial,
                typeMeta.Syntax.Identifier.GetLocation(),
                typeMeta.Symbol.Name));
            error = true;
        }

        // nested is not allowed
        if (typeMeta.IsNested())
        {
            diagnostics.Add(DiagnosticInfo.Create(
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
                diagnostics.Add(DiagnosticInfo.Create(
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
                diagnostics.Add(DiagnosticInfo.Create(
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
                diagnostics.Add(DiagnosticInfo.Create(
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
                    diagnostics.Add(DiagnosticInfo.Create(
                        DiagnosticDescriptors.RedundantInterceptorType,
                        x.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? typeMeta.Syntax.GetLocation(),
                        x.TypeSymbol.Name));
                    error = true;
                }
            }
        }

        var ns = typeMeta.Symbol.ContainingNamespace;
        var hintName = typeMeta.FullTypeName
            .Replace("global::", "")
            .Replace("<", "_")
            .Replace(">", "_");

        return new TypeModel(
            TypeName: typeMeta.TypeName,
            FullTypeName: typeMeta.FullTypeName,
            Namespace: ns.IsGlobalNamespace ? null : ns.ToDisplayString(),
            HintName: hintName,
            InheritsMonoBehaviour: references.MonoBehaviourType is not null && typeMeta.Symbol.InheritsFrom(references.MonoBehaviourType),
            HasError: error,
            Routes: typeMeta.RouteMethodMetas.Select(ToModel).ToArray(),
            DefaultInterceptors: typeMeta.DefaultInterceptorMetas.Select(ToModel).ToArray(),
            AllInterceptors: typeMeta.AllInterceptorMetas.Select(ToModel).ToArray(),
            Diagnostics: diagnostics.ToArray());
    }

    static RouteModel ToModel(RouteMethodMeta meta) => new(
        MethodName: meta.Symbol.Name,
        CommandFullTypeName: meta.CommandFullTypeName,
        CommandTypePrefix: meta.CommandTypePrefix,
        IsAsync: meta.IsAsync,
        IsValueTask: meta.IsValueTask(),
        IsUniTask: meta.IsUniTask(),
        TakeCancellationToken: meta.TakeCancellationToken,
        TakePublishContext: meta.TakePublishContext,
        Interceptors: meta.InterceptorMetas.Select(ToModel).ToArray());

    static InterceptorModel ToModel(InterceptorMeta meta) => new(meta.FullTypeName, meta.VariableName);
}
