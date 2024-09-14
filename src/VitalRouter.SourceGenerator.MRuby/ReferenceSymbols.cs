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
            CommandInterface = compilation.GetTypeByMetadataName("VitalRouter.ICommand")!,
            MessagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute")!,
        };
    }

    public INamedTypeSymbol MRubyCommandAttribute { get; private set; } = default!;
    public INamedTypeSymbol MRubyCommandPresetType { get; private set; } = default!;
    public INamedTypeSymbol CommandInterface { get; private set; } = default!;
    public INamedTypeSymbol MessagePackObjectAttribute { get; private set; } = default!;
}
