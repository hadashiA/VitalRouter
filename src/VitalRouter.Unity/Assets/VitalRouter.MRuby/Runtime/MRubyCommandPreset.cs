using System.Threading;
using Cysharp.Threading.Tasks;

namespace VitalRouter.MRuby
{
    public abstract class MRubyCommandPreset
    {
        public abstract UniTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            MrbValue payload,
            CancellationToken cancellation = default);
    }
}