#pragma warning disable RS2008

using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator;

static class DiagnosticDescriptors
{
    const string Category = "VitalRouter.SourceGenerator";

    public static readonly DiagnosticDescriptor UnexpectedErrorDescriptor = new(
        id: "VITALROUTER001",
        title: "Unexpected error during source code generation",
        messageFormat: "Unexpected error occurred during source code code generation: {0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "VITALROUTER002",
        title: "VitalRouter routing taregt type declaration must be partial",
        messageFormat: "The VitalRouter routable type declaration '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedNotAllow = new(
        id: "VITALROUTER003",
        title: "VitalRouter routing target type must not be nested type",
        messageFormat: "The VitalRouter routable object '{0}' must be not nested type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoRoutablePublicMethodDefined = new(
        id: "VITALROUTER004",
        title: "The VitalRouter routing target has a non routable public method",
        messageFormat: "The public method '{0}' of the [Routing] class of VitalRouter is not the target",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
