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
        title: "MRuby type declaration must be partial",
        messageFormat: "The implementation of type '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedNotAllow = new(
        id: "VRMRB003",
        title: "MRuby type must not be nested type",
        messageFormat: "The implementation of type '{0}' must not be nested type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AbstractNotAllow = new(
        id: "VRMRB004",
        title: "MRuby type must not be abstract or interface",
        messageFormat: "The implementation of type '{0}' must not be abstract or interface type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPresetType = new(
        id: "VRMRB005",
        title: "CommandPreset type must inherit VitalRouter.MRuby.MRubyCommandPreset",
        messageFormat: "'{0}' must inherit VitalRouter.MRuby.MRubyCommandPreset",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCommandType = new(
        id: "VRMRB006",
        title: "Command type must implements VitalRouter.ICommand",
        messageFormat: "The VitalRouter command type '{0}' must implements VitalRouter.ICommand",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBeSerializable = new(
        id: "VRMRB007",
        title: "",
        messageFormat: "The VitalRouter command type '{0}' must mark as  [MRubyObject]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static readonly DiagnosticDescriptor MRubyObjectPropertyMustHaveSetter = new(
        id: "VRMRB008",
        title: "A mruby serializable property with must have setter",
        messageFormat: "The MRubyObject '{0}' property '{1}' must have setter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MRubyObjectFieldCannotBeReadonly = new(
        id: "VRMRB009",
        title: "A mruby serializable field cannot be readonly",
        messageFormat: "The MRubyObject '{0}' field '{1}' cannot be readonly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static readonly DiagnosticDescriptor MultipleConstructorAttribute = new(
        id: "VRMRB010",
        title: "[MRubyConstructor] exists in multiple constructors",
        messageFormat: "Multiple [MRubyConstructor] exists in '{0}' but allows only single ctor",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleConstructorWithoutAttribute = new(
        id: "VRMRB011",
        title: "Require [MRubyConstructor] when exists multiple constructors",
        messageFormat: "The MRubyObject '{0}' must annotate with [MRubyConstructor] when exists multiple constructors",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConstructorHasNoMatchedParameter = new(
        id: "VRMRB0012",
        title: "MRubyObject's constructor has no matched parameter",
        messageFormat: "The MRubyObject '{0}' constructor's parameter '{1}' must match a serialized member name(case-insensitive)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

}
