#pragma warning disable RS2008

using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

static class DiagnosticDescriptors
{
    const string Category = "VitalRouter.SourceGenerator.MRuby";

    public static readonly DiagnosticDescriptor UnexpectedErrorDescriptor = new(
        id: "VRMRB001",
        title: "Unexpected error during source code generation",
        messageFormat: "Unexpected error occurred during source code code generation: {0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "VRMRB002",
        title: "MRubyPreset type declaration must be partial",
        messageFormat: "The implementation of type '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedNotAllow = new(
        id: "VRMRB003",
        title: "MRubyPreset type must not be nested type",
        messageFormat: "The implementation of type '{0}' must be not nested type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static readonly DiagnosticDescriptor InvalidPresetType = new(
        id: "VRMRB004",
        title: "CommandPreset type must inherit VitalRouter.MRuby.MRubyCommandPreset",
        messageFormat: "'{0}' must inherit VitalRouter.MRuby.MRubyCommandPreset",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCommandType = new(
        id: "VRMRB005",
        title: "Command type must implements VitalRouter.ICommand",
        messageFormat: "The VitalRouter command type '{0}' must implements VitalRouter.ICommand",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBeSerializable = new(
        id: "VRMRB006",
        title: "",
        messageFormat: "The VitalRouter command type '{0}' must mark as  [MessagePackObject(keyAsPropertyName: true)]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
