using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace VitalRouter.SourceGenerator.MRuby;

[Generator]
public class VitalRouterMRubyIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandPresetProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                context,
                "VitalRouter.MRuby.MRubyCommandAttribute",
                static (node, cancellation) => node is ClassDeclarationSyntax,
                static (context, cancellation) => context);
            // .Combine(context.CompilationProvider);
            // .WithComparer(Comparer.Instance);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(commandPresetProvider.Collect()),
            (productionContext, t) =>
            {
                var (compilation, list) = t;
                var references = ReferenceSymbols.Create(compilation);
                if (references is null)
                {
                    return;
                }

                var stringBuilder = new StringBuilder();

                foreach (var x in list)
                {
                    var typeMeta = new CommandPresetTypeMeta(
                        (TypeDeclarationSyntax)x.TargetNode,
                        (INamedTypeSymbol)x.TargetSymbol,
                        x.Attributes);

                    if (TryEmitCommandPreset(typeMeta, references, stringBuilder, productionContext))
                    {
                        var fullType = typeMeta.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", "")
                            .Replace("<", "_")
                            .Replace(">", "_");

                        productionContext.AddSource($"{fullType}.g.cs", stringBuilder.ToString());
                    }
                    stringBuilder.Clear();
                }
            });

        var serializableProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                context,
                "VitalRouter.MRuby.MRubyObjectAttribute",
                static (node, cancellation) =>
                    node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
                static (context, cancellation) => context);
            // .Combine(context.CompilationProvider);
            //.WithComparer(Comparer.Instance)

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(serializableProvider.Collect()),
            (productionContext, t) =>
            {
                var (compilation, list) = t;
                var references = ReferenceSymbols.Create(compilation);
                if (references is null)
                {
                    return;
                }

                var stringBuilder = new StringBuilder();

                foreach (var x in list)
                {
                    var typeMeta = new MRubyObjectTypeMeta(
                        (TypeDeclarationSyntax)x.TargetNode,
                        (INamedTypeSymbol)x.TargetSymbol,
                        x.Attributes.First(),
                        references);

                    if (TryEmitMRubyObjectType(typeMeta, stringBuilder, references, productionContext))
                    {
                        var fullType = typeMeta.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", "")
                            .Replace("<", "_")
                            .Replace(">", "_");

                        productionContext.AddSource($"{fullType}.g.cs", stringBuilder.ToString());
                    }
                    stringBuilder.Clear();
                }
            });
    }

    static bool TryEmitMRubyObjectType(
        MRubyObjectTypeMeta typeMeta,
        StringBuilder stringBuilder,
        ReferenceSymbols references,
        in SourceProductionContext context)
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

            // verify abstract/interface
            if (typeMeta.Symbol.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AbstractNotAllow,
                    typeMeta.Syntax.Identifier.GetLocation(),
                    typeMeta.TypeName));
                error = true;
            }

            if (error)
            {
                return false;
            }

            stringBuilder.AppendLine($$"""
// <auto-generated />
#nullable enable
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602 // Possible null return
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619 // Possible null reference assignment fix
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method

using System;
using VitalRouter;
using VitalRouter.MRuby;
""");

            var ns = typeMeta.Symbol.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                stringBuilder.AppendLine($$"""
namespace {{ns}}
{
""");
            }

            var typeDeclarationKeyword = (typeMeta.Symbol.IsRecord, typeMeta.Symbol.IsValueType) switch
            {
                (true, true) => "record struct",
                (true, false) => "record",
                (false, true) => "struct",
                (false, false) => "class",
            };

            stringBuilder.AppendLine($$"""
partial {{typeDeclarationKeyword}} {{typeMeta.TypeName}}
{
""");
                // EmitCCtor(typeMeta, codeWriter, in context);
                if (!TryEmitRegisterMethod(typeMeta, stringBuilder, in context))
                {
                    return false;
                }
                if (!TryEmitFormatter(typeMeta, stringBuilder, references, in context))
                {
                    return false;
                }

            stringBuilder.AppendLine("}");
            if (!ns.IsGlobalNamespace)
            {
                stringBuilder.AppendLine("}");
            }

            stringBuilder.AppendLine($$"""
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

    static bool TryEmitFormatter(
        MRubyObjectTypeMeta typeMeta,
        StringBuilder stringBuilder,
        ReferenceSymbols references,
        in SourceProductionContext context)
    {
        var returnType = typeMeta.Symbol.IsValueType
            ? typeMeta.FullTypeName
            : $"{typeMeta.FullTypeName}?";

        stringBuilder.AppendLine($$"""
    [global::VitalRouter.Preserve]
    public class {{typeMeta.TypeName}}GeneratedFormatter : IMrbValueFormatter<{{returnType}}>
    {
""");
        // Default
        foreach (var memberMeta in typeMeta.MemberMetas)
        {
            stringBuilder.Append($$"""
        static readonly byte[] {{memberMeta.Name}}KeyUtf8 =
""");
            stringBuilder.AppendByteArrayString(memberMeta.KeyNameUtf8Bytes);
            stringBuilder.AppendLine($"; // {memberMeta.KeyName}");
        }

        var result = TryEmitDeserializeMethod(typeMeta, stringBuilder, references, in context);
        stringBuilder.AppendLine($$"""
    }
""");
        return result;
    }

    static bool TryEmitRegisterMethod(MRubyObjectTypeMeta typeMeta, StringBuilder stringBuilder, in SourceProductionContext context)
    {
        stringBuilder.AppendLine($$"""
    [global::VitalRouter.Preserve]
    public static void __RegisterMrbValueFormatter()
    {
        global::VitalRouter.MRuby.GeneratedResolver.Register(new {{typeMeta.TypeName}}GeneratedFormatter());
    }

""");
        return true;
    }

    static bool TryEmitDeserializeMethod(
        MRubyObjectTypeMeta typeMeta,
        StringBuilder stringBuilder,
        ReferenceSymbols references,
        in SourceProductionContext context)
    {
        if (!TryGetConstructor(typeMeta, references, in context,
                out var selectedConstructor,
                out var constructedMembers))
        {
            return false;
        }

        var setterMembers = typeMeta.MemberMetas
            .Where(x =>
            {
                return constructedMembers.All(constructedMember => !SymbolEqualityComparer.Default.Equals(x.Symbol, constructedMember.Symbol));
            })
            .ToArray();

        foreach (var setterMember in setterMembers)
        {
            switch (setterMember)
            {
                case { IsProperty: true, IsSettable: false }:
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MRubyObjectPropertyMustHaveSetter,
                        setterMember.GetLocation(typeMeta.Syntax),
                        typeMeta.TypeName,
                        setterMember.Name));
                    return false;
                }
                case { IsField: true, IsSettable: false }:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MRubyObjectFieldCannotBeReadonly,
                        setterMember.GetLocation(typeMeta.Syntax),
                        typeMeta.TypeName,
                        setterMember.Name));
                    return false;
            }
        }

        var returnType = typeMeta.Symbol.IsValueType
            ? typeMeta.FullTypeName
            : $"{typeMeta.FullTypeName}?";
        stringBuilder.AppendLine($$"""
        [global::VitalRouter.Preserve]
        public {{returnType}} Deserialize(MrbValue mrbValue, MRubyContext context, MrbValueSerializerOptions options)
        {
            if (mrbValue.IsNil)
            {
                return default;
            }

""");
        if (typeMeta.MemberMetas.Count <= 0)
        {
            stringBuilder.AppendLine($$"""
            return new {{typeMeta.TypeName}}();
        }
""");
            return true;
        }

        stringBuilder.AppendLine($$"""
            MRubySerializationException.ThrowIfTypeMismatch(mrbValue, MrbVtype.MRB_TT_HASH, "{{typeMeta.TypeName}}");
""");
        foreach (var memberMeta in typeMeta.MemberMetas)
        {
            stringBuilder.AppendLine($$"""
            var __{{memberMeta.Name}}__ = {{memberMeta.EmitDefaultValue()}};
""");
        }

        stringBuilder.AppendLine($$"""
            foreach (var entry in mrbValue.AsHashEnumerable(context))
            {
                var key = options.Resolver.GetFormatterWithVerify<global::VitalRouter.MRuby.FixedUtf8String>().Deserialize(entry.Key, context, options);
""");

        var membersByNameLength = typeMeta.MemberMetas.GroupBy(x => x.KeyNameUtf8Bytes.Length);

        foreach (var group in membersByNameLength)
        {
            var branching = "if";
            foreach (var memberMeta in group)
            {
                stringBuilder.AppendLine($$"""
                {{branching}} (key.EquivalentIgnoreCaseTo({{memberMeta.Name}}KeyUtf8))
                {
                    __{{memberMeta.Name}}__ = options.Resolver.GetFormatterWithVerify<{{memberMeta.FullTypeName}}>()
                        .Deserialize(entry.Value, context, options);
                    continue;
                }
""");
                branching = "else if";
            }
        }
        stringBuilder.AppendLine($$"""
            }
""");

        stringBuilder.Append($$"""
            return new {{typeMeta.TypeName}}
""");
        if (selectedConstructor != null)
        {
            var parameters = string.Join(",", constructedMembers.Select(x => $"__{x.Name}__"));
            stringBuilder.AppendLine($"({parameters})");
        }
        else
        {
            stringBuilder.AppendLine("()");
        }
        if (setterMembers.Length > 0)
        {
            stringBuilder.AppendLine($$"""
            {
""");

            foreach (var setterMember in setterMembers)
            {
                if (!constructedMembers.Contains(setterMember))
                {
                    stringBuilder.AppendLine($$"""
                {{setterMember.Name}} = __{{setterMember.Name}}__,
""");
                }
            }
            stringBuilder.AppendLine($$"""
            }
""");
        }
        stringBuilder.AppendLine($$"""
            ;
        }
""");
        return true;
    }

    static bool TryEmitCommandPreset(CommandPresetTypeMeta commandPresetTypeMeta, ReferenceSymbols referenceSymbols, StringBuilder builder, in SourceProductionContext context)
    {
        var error = false;

        // verify is partial
        if (!commandPresetTypeMeta.IsPartial())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MustBePartial,
                commandPresetTypeMeta.Syntax.Identifier.GetLocation(),
                commandPresetTypeMeta.Symbol.Name));
            error = true;
        }

        // nested is not allowed
        if (commandPresetTypeMeta.IsNested())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NestedNotAllow,
                commandPresetTypeMeta.Syntax.Identifier.GetLocation(),
                commandPresetTypeMeta.Symbol.Name));
            error = true;
        }

        if (!commandPresetTypeMeta.Symbol.InheritsFrom(referenceSymbols.MRubyCommandPresetType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidPresetType,
                commandPresetTypeMeta.Syntax.GetLocation(),
                commandPresetTypeMeta.Symbol.Name));
            error = true;
        }

        // Check CommandType
        foreach (var commandMeta in commandPresetTypeMeta.CommandMetas)
        {
            if (!commandMeta.CommandType.AllInterfaces.Any(x =>
                {
                    return SymbolEqualityComparer.Default.Equals(x, referenceSymbols.CommandInterface);
                }))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidCommandType,
                    commandMeta.Syntax.GetLocation(),
                    commandMeta.CommandType.Name));
                error = true;
            }
            if (!commandMeta.IsMRubyObject(referenceSymbols))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MustBeSerializable,
                    commandMeta.Syntax.GetLocation(),
                    commandMeta.CommandType.Name));
                error = true;
            }
        }

        if (error)
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
#pragma warning disable CS8631 // The type cannot be used as type parameter in
 
using System;
using System.Collections.Generic;
using System.Threading;
using VitalRouter;
using VitalRouter.MRuby;
""");
        var ns = commandPresetTypeMeta.Symbol.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            builder.AppendLine($$"""
namespace {{ns}} 
{
""");
        }
        builder.AppendLine($$"""
partial class {{commandPresetTypeMeta.TypeName}}
{
""");

        builder.AppendLine($$"""
    static readonly Dictionary<global::VitalRouter.MRuby.FixedUtf8String, int> __Names = new()
    {
""");
        for (var i = 0; i < commandPresetTypeMeta.CommandMetas.Count; i++)
        {
            builder.AppendLine($$"""
        { new global::VitalRouter.MRuby.FixedUtf8String("{{commandPresetTypeMeta.CommandMetas[i].Key}}"), {{i}} },
""");
        }
        builder.AppendLine($$"""
    };
    
    public override async global::Cysharp.Threading.Tasks.UniTask CommandCallFromMrubyAsync(
        global::VitalRouter.MRuby.MRubyScript script,
        global::VitalRouter.MRuby.FixedUtf8String commandName,
        global::VitalRouter.MRuby.MrbValue payload,
        global::System.Threading.CancellationToken cancellation = default)
    {
        try
        {
            if (!__Names.TryGetValue(commandName, out var index))
            {
                script.Fail(new ArgumentOutOfRangeException(nameof(commandName), $"No such command `{commandName}` in {GetType().Name}. Please use `[MRubyCommand(...)]` attribute and register it."));
                return;
            }

            switch (index)
            {
""");
        for (var i = 0; i < commandPresetTypeMeta.CommandMetas.Count; i++)
        {
            builder.AppendLine($$"""
                case {{i}}:
                {
                    var cmd = MrbValueSerializer.Deserialize<{{commandPresetTypeMeta.CommandMetas[i].CommandType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>(payload, script.Context);
                    await script.Context.Publisher.PublishAsync(cmd, cancellation);
                    break;
                }
""");
        }
        builder.Append($$"""
            }
            script.Resume();
        }
        catch (Exception ex)
        {
            script.Fail(ex);
        }
    }
}
""");
        if (!ns.IsGlobalNamespace)
        {
            builder.AppendLine("}");
        }

        return true;
    }

    static bool TryGetConstructor(
        MRubyObjectTypeMeta typeMeta,
        ReferenceSymbols reference,
        in SourceProductionContext context,
        out IMethodSymbol? selectedConstructor,
        out IReadOnlyList<MRubyObjectMemberMeta> constructedMembers)
    {
        if (typeMeta.Constructors.Count <= 0)
        {
            selectedConstructor = null;
            constructedMembers = Array.Empty<MRubyObjectMemberMeta>();
            return true;
        }

        if (typeMeta.Constructors.Count == 1)
        {
            selectedConstructor = typeMeta.Constructors[0];
        }
        else
        {
            var ctorWithAttrs = typeMeta.Constructors
                .Where(x => x.ContainsAttribute(reference.MRubyConstructorAttribute))
                .ToArray();

            switch (ctorWithAttrs.Length)
            {
                case 1:
                    selectedConstructor = ctorWithAttrs[0];
                    break;
                case > 1:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MultipleConstructorAttribute,
                        typeMeta.Syntax.Identifier.GetLocation(),
                        typeMeta.Symbol.Name));
                    selectedConstructor = null;
                    constructedMembers = [];
                    return false;

                default:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MultipleConstructorWithoutAttribute,
                        typeMeta.Syntax.Identifier.GetLocation(),
                        typeMeta.Symbol.Name));
                    selectedConstructor = null;
                    constructedMembers = [];
                    return false;
            }
        }

        var parameterMembers = new List<MRubyObjectMemberMeta>();
        var error = false;
        foreach (var parameter in selectedConstructor.Parameters)
        {
            var matchedMember = typeMeta.MemberMetas
                .FirstOrDefault(member => parameter.Name.Equals(member.Name, StringComparison.OrdinalIgnoreCase));
            if (matchedMember != null)
            {
                matchedMember.IsConstructorParameter = true;
                if (parameter.HasExplicitDefaultValue)
                {
                    matchedMember.HasExplicitDefaultValueFromConstructor = true;
                    matchedMember.ExplicitDefaultValueFromConstructor = parameter.ExplicitDefaultValue;
                }
                parameterMembers.Add(matchedMember);
            }
            else
            {
                var location = selectedConstructor.Locations.FirstOrDefault() ??
                               typeMeta.Syntax.Identifier.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ConstructorHasNoMatchedParameter,
                    location,
                    typeMeta.Symbol.Name,
                    parameter.Name));
                constructedMembers = [];
                error = true;
            }
        }
        constructedMembers = parameterMembers;
        return !error;
    }

}