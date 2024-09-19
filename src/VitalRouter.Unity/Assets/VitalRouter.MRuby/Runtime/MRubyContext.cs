using System;
using System.Runtime.InteropServices;

namespace VitalRouter.MRuby
{
    public class MRubyScriptCompileException : Exception
    {
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

        internal unsafe MRubyContext(MrbContextCore* state) : base((IntPtr)state, true)
        {
            SharedState = new MRubySharedState(this);
        }

        public void Load(string rubySource)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            Load(bytes);
        }

        public unsafe void Load(ReadOnlySpan<byte> rubySource)
        {
            EnsureNotDisposed();
            fixed (byte* ptr = rubySource)
            {
                var source = new MrbNString
                {
                    Bytes = ptr,
                    Length = rubySource.Length
                };
                NativeMethods.MrbLoad(DangerousGetPtr(), source);
            }
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