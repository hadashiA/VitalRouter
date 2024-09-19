using Microsoft.CodeAnalysis;

namespace VitalRouter.SourceGenerator.MRuby;

public class ReferenceSymbols
{
    public static ReferenceSymbols? Create(Compilation compilation)
    {
        var mRubyCommandAttribute = compilation.GetTypeByMetadataName("VitalRouter.MRuby.MRubyCommandAttribute");
        if (mRubyCommandAttribute is null)
            return null;

        return new ReferenceSymbols
        {
            MRubyCommandAttribute = mRubyCommandAttribute,
            MRubyCommandPresetType = compilation.GetTypeByMetadataName("VitalRouter.MRuby.MRubyCommandPreset")!,
            MRubyObjectAttribute = compilation.GetTypeByMetadataName("VitalRouter.MRuby.MRubyObjectAttribute")!,
            MRubyMemberAttribute = compilation.GetTypeByMetadataName("VitalRouter.MRuby.MRubyMemberAttribute")!,
            MRubyIgnoreAttribute = compilation.GetTypeByMetadataName("VitalRouter.MRuby.MRubyIgnoreAttribute")!,
            MRubyConstructorAttribute = compilation.GetTypeByMetadataName("MessagePack.MRubyConstructorAttribute")!,
            CommandInterface = compilation.GetTypeByMetadataName("VitalRouter.ICommand")!,
        };
    }

    public INamedTypeSymbol MRubyCommandAttribute { get; private set; } = default!;
    public INamedTypeSymbol MRubyCommandPresetType { get; private set; } = default!;
    public INamedTypeSymbol MRubyObjectAttribute { get; private set; } = default!;
    public INamedTypeSymbol MRubyMemberAttribute { get; private set; } = default!;
    public INamedTypeSymbol MRubyIgnoreAttribute { get; private set; } = default!;
    public INamedTypeSymbol MRubyConstructorAttribute { get; private set; } = default!;
    public INamedTypeSymbol CommandInterface { get; private set; } = default!;
}
