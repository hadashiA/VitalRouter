using System;
using System.Runtime.CompilerServices;
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

    static unsafe class MRubyAllocator
    {
        const Allocator DefaultAllocator = Allocator.Persistent;
        static readonly long HeaderSize = UnsafeUtility.SizeOf<Header>();

        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            public uint Size;
        }

        [MonoPInvokeCallback(typeof(MrbAllocF))]
        internal static void* AllocPersistent(void* mrb, void* ptr, nuint size, void* ud)
        {
            if (size == 0 && ptr != null)
            {
                Free(ptr);
                return null;
            }

            var newPtr = Malloc((uint)size);
            if (ptr != null)
            {
                // realloc
                ReadHeader(ptr, out var currentSize);
                if (currentSize >= size) return ptr;

                UnsafeUtility.MemCpy(newPtr, ptr, currentSize);
                Free(ptr);
            }
            return newPtr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Free(void* ptr)
        {
            UnsafeUtility.Free((byte*)ptr - HeaderSize, DefaultAllocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void* Malloc(uint size)
        {
            var allocated = UnsafeUtility.Malloc(size + HeaderSize, sizeof(byte), DefaultAllocator);
            WriteHeader(allocated, size);
            return (byte*)allocated + HeaderSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ReadHeader(void* ptr, out uint size)
        {
            size = ((Header*)((byte*)ptr - HeaderSize))->Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteHeader(void* ptr, uint size)
        {
            ((Header*)ptr)->Size = size;
        }
    }

    public class MRubyContext : SafeHandle
    {
        public static unsafe MRubyContext Create()
        {
            NativeMethods.MrbAllocfSet(MRubyAllocator.AllocPersistent);

            var ptr = NativeMethods.MrbContextNew();
            NativeMethods.MrbCallbacksSet(ptr, MRubyScript.OnCommandCalled, MRubyScript.OnError);

            return new MRubyContext(ptr);
        }

        public static MRubyContext Create(Router router, MRubyCommandPreset commandPreset)
        {
            var context = Create();
            context.Router = router;
            context.CommandPreset = commandPreset;
            return context;
        }

        public static Action<Exception> GlobalErrorHandler { get; set; } = UnityEngine.Debug.LogError;
        public static Action<string> GlobalLogHandler { get; set; } = UnityEngine.Debug.Log;

        public MRubySharedState SharedState { get; }

        public Router Router
        {
            get
            {
                if (router == null)
                {
                    Router = Router.Default;
                }
                return router!;
            }
            set
            {
                router?.RemoveFilter(x =>
                    x is MRubyContextInterceptor interceptor && interceptor.MrubyContext == this);
                router = value.Filter(new MRubyContextInterceptor(this));
            }
        }

        public ICommandPublisher Publisher => Router;

        public MRubyCommandPreset? CommandPreset { get; set; }

        public override bool IsInvalid => handle == IntPtr.Zero;
        Router? router;

#pragma warning disable CS8500
#pragma warning disable CS8981

        internal unsafe MrbContextCore* DangerousGetPtr() => (MrbContextCore*)DangerousGetHandle();

        unsafe MRubyContext(MrbContextCore* state) : base((IntPtr)state, true)
        {
            SharedState = new MRubySharedState(this);
            Router = Router.Default;
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
            if (IsClosed || IsInvalid)
            {
                throw new ObjectDisposedException("MRubyContext is already disposed.");
            }
        }
    }
}