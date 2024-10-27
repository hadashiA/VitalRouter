using System.Threading;
using System.Threading.Tasks;

namespace VitalRouter.MRuby
{
    public class NoneMRubyCommandPreset : MRubyCommandPreset
    {
        public override ValueTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            MrbValue payload,
            CancellationToken cancellation = default) => default;
    }

    public abstract class MRubyCommandPreset
    {
        public static readonly MRubyCommandPreset None = new NoneMRubyCommandPreset();

        public abstract ValueTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            MrbValue payload,
            CancellationToken cancellation = default);
    }
}
