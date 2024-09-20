using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.MRuby
{
    public class NoneMRubyCommandPreset : MRubyCommandPreset
    {
        public override UniTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            MrbValue payload,
            CancellationToken cancellation = default) => UniTask.CompletedTask;
    }

    public abstract class MRubyCommandPreset
    {
        public static readonly MRubyCommandPreset None = new NoneMRubyCommandPreset();

        public abstract UniTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            MrbValue payload,
            CancellationToken cancellation = default);
    }
}
