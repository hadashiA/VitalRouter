using System;
using System.Runtime.InteropServices;

namespace VitalRouter.MRuby
{
    public class MRubyScriptCompileException : Exception
    {
    }

    public readonly struct MrbValueHandle : IDisposable
    {
        public readonly MrbValue RawValue;
        readonly MRubyContext context;

        public MrbValueHandle(MrbValue rawValue, MRubyContext context)
        {
            RawValue = rawValue;
            this.context = context;
        }

        public unsafe void Dispose()
        {
            if (context.IsInvalid || context.IsClosed)
            {
                return;
            }
            NativeMethods.MrbValueRelease(context.DangerousGetPtr(), RawValue);
        }
    }

    public class MRubyContext : SafeHandle
    {
        public static unsafe MRubyContext Create(Router publisher, MRubyCommandPreset commandPreset)
        {
            var ptr = NativeMethods.MrbContextNew();
            NativeMethods.MrbCallbacksSet(ptr, MRubyScript.OnCommandCalled, MRubyScript.OnError);

            var context = new MRubyContext(ptr)
            {
                Publisher = publisher,
                CommandPreset = commandPreset,
            };

            publisher.Filter(new MRubyContextInterceptor(context));
            return context;
        }

        public static Action<Exception> GlobalErrorHandler { get; set; } = UnityEngine.Debug.LogError;
        public static Action<string> GlobalLogHandler { get; set; } = UnityEngine.Debug.Log;

        public MRubySharedState SharedState { get; }
        public ICommandPublisher Publisher { get; set; } = default!;
        public MRubyCommandPreset CommandPreset { get; set; } = default!;

        public override bool IsInvalid => handle == IntPtr.Zero;

#pragma warning disable CS8500
#pragma warning disable CS8981

        internal unsafe MrbContextCore* DangerousGetPtr() => (MrbContextCore*)DangerousGetHandle();

        unsafe MRubyContext(MrbContextCore* state) : base((IntPtr)state, true)
        {
            SharedState = new MRubySharedState(this);
        }

        public void Load(string rubySource)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            Load(bytes);
        }

        public void Load(ReadOnlySpan<byte> rubySource)
        {
            EvaluateUnsafe(rubySource).Dispose();
        }

        public T? Evaluate<T>(string rubySource)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            return Evaluate<T>(bytes);
        }

        public T? Evaluate<T>(ReadOnlySpan<byte> rubySource)
        {
            using var result = EvaluateUnsafe(rubySource);
            return MrbValueSerializer.Deserialize<T>(result.RawValue, this);
        }

        public MrbValueHandle EvaluateUnsafe(string rubySource)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            return EvaluateUnsafe(bytes);
        }

        public unsafe MrbValueHandle EvaluateUnsafe(ReadOnlySpan<byte> rubySource)
        {
            EnsureNotDisposed();
            MrbValue resultValue;
            fixed (byte* ptr = rubySource)
            {
                var source = new MrbNString
                {
                    Bytes = ptr,
                    Length = rubySource.Length
                };
                resultValue = NativeMethods.MrbLoad(DangerousGetPtr(), source);
            }
            return new MrbValueHandle(resultValue, this);
        }

        public MRubyScript CompileScript(string rubySource)
        {
            EnsureNotDisposed();
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            return CompileScript(bytes);
        }

        public unsafe MRubyScript CompileScript(ReadOnlySpan<byte> rubySource)
        {
            EnsureNotDisposed();
            fixed (byte* ptr = rubySource)
            {
                var source = new MrbNString
                {
                    Bytes = ptr,
                    Length = rubySource.Length
                };
                var scriptPtr = NativeMethods.ScriptCompile(DangerousGetPtr(), source);
                if (scriptPtr == null)
                {
                    throw new MRubyScriptCompileException();
                }

                var script = new MRubyScript(this, scriptPtr);
                MRubyScript.Scripts.TryAdd(script.ScriptId, script);
                return script;
            }
        }

#pragma warning restore CS8500
#pragma warning restore CS8981

        protected override unsafe bool ReleaseHandle()
        {
            if (IsClosed) return false;
            NativeMethods.MrbContextDispose(DangerousGetPtr());
            return true;
        }

        void EnsureNotDisposed()
        {
            if (IsClosed && IsInvalid)
            {
                throw new ObjectDisposedException("MRubyContext is already disposed.");
            }
        }
    }
}