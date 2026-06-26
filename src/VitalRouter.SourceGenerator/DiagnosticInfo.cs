using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace VitalRouter.SourceGenerator;

/// <summary>
/// A cacheable stand-in for <see cref="Location"/>. <see cref="Location"/> roots a
/// <c>SyntaxTree</c> and is not reliably value-equatable, so it must not live in the
/// incremental model. We keep only the file path and spans, and rebuild the location
/// at emit time.
/// </summary>
sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? CreateFrom(Location? location)
    {
        if (location?.SourceTree is null)
        {
            return null;
        }
        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}

/// <summary>
/// A cacheable diagnostic. <see cref="DiagnosticDescriptor"/> instances are static
/// singletons (value-equatable), and the message arguments are plain strings, so this
/// is safe to store in the incremental model and replay during source output.
/// </summary>
sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo? Location,
    EquatableArray<string> MessageArgs)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
        => new(descriptor, LocationInfo.CreateFrom(location), messageArgs);

    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(Descriptor, Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, MessageArgs.ToArray());
}
