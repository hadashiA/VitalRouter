using System;
using System.Runtime.InteropServices;
using AOT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

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

    static class MRubyAllocator
    {
        const Allocator DefaultAllocator = Allocator.Persistent;
        static readonly long HeaderSize = UnsafeUtility.SizeOf<Header>();

        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            public ulong Size;
        }

        [MonoPInvokeCallback(typeof(MrbAllocF))]
        internal static unsafe void* AllocPersistent(void* mrb, void* ptr, nuint size, void* ud)
        {
            // free
            if (size == 0 && ptr != null)
            {
                UnsafeUtility.Free((byte*)ptr - HeaderSize, DefaultAllocator);
                return null;
            }

            // malloc
            if (ptr == null)
            {
                var newPtr = UnsafeUtility.Malloc((long)size + HeaderSize, sizeof(byte), DefaultAllocator);
                ((Header*)newPtr)->Size = size;
                return (byte*)newPtr + HeaderSize;
            }
            else
            {
                // realloc
                var currentHeader = *(Header*)((byte*)ptr - HeaderSize);
                if (currentHeader.Size >= size)
                {
                    return ptr;
                }

                var newPtr = UnsafeUtility.Malloc((long)size + HeaderSize, sizeof(byte), DefaultAllocator);
                ((Header*)newPtr)->Size = size;

                var dst = (byte*)newPtr + HeaderSize;
                UnsafeUtility.MemCpy(dst, ptr, (long)currentHeader.Size);
                UnsafeUtility.Free((byte*)ptr - HeaderSize, DefaultAllocator);
                return dst;
            }
        }
    }

    public class MRubyContext : SafeHandle
    {
        public static unsafe MRubyContext Create(Router publisher, MRubyCommandPreset commandPreset, Allocator allocator = Allocator.Persistent)
        {
            NativeMethods.MrbAllocfSet(MRubyAllocator.AllocPersistent);

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

        public T? Evaluate<T>(string rubySource, MrbValueSerializerOptions options = null)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rubySource);
            return Evaluate<T>(bytes);
        }

        public T? Evaluate<T>(ReadOnlySpan<byte> rubySource, MrbValueSerializerOptions options = null)
        {
            using var result = EvaluateUnsafe(rubySource);
            return MrbValueSerializer.Deserialize<T>(result.RawValue, this, options);
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