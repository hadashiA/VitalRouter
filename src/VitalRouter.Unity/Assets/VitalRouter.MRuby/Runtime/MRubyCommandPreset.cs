using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;

namespace VitalRouter.MRuby
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MRubyCommandAttribute : Attribute
    {
        public string Key { get; }
        public Type CommandType { get; }

        public MRubyCommandAttribute(string key, Type commandType)
        {
            Key = key;
            CommandType = commandType;
        }
    }

    public abstract class MRubyCommandPreset
    {
        public abstract ValueTask CommandCallFromMrubyAsync(
            MRubyScript script,
            FixedUtf8String commandName,
            NativeArray<byte> payload,
            CancellationToken cancellation = default);
    }
}