using System;
using System.Runtime.InteropServices;

namespace VitalRouter.MRuby
{
    enum NativeMethodResult
    {
        Ok =       0,
        Error =   -1,
        Continue = 1,
        Done     = 2,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void MrbCommandHandler(int scriptId, MrbNString commandName, MrbValue payload);

    unsafe delegate void MrbErrorHandler(int scriptId, byte* message);

    // typedef void* (*mrb_allocf) (struct mrb_state *mrb, void *ptr, size_t size, void *ud);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void* MrbAllocF(void *mrb, void *ptr, nuint size, void *ud);

    [StructLayout(LayoutKind.Sequential)]
    struct MrbContextCore
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MrbScriptCore
    {
        public int Id;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct MrbNString
    {
        public byte* Bytes;
        public int Length;
    }

    // mruby types

    [StructLayout(LayoutKind.Explicit)]
    public struct MrbValueUnion
    {
        // Assuming MRB_NO_FLOAT is off, MRB_USE_FLOAT is off.
        [FieldOffset(0)]
        public double F;

        [FieldOffset(0)]
        public nint I;

        [FieldOffset(0)]
        public IntPtr P;

        [FieldOffset(0)]
        public uint Sym;
    }

    // NOTE: Assuming MRUBY_BOXING_NO
    [StructLayout(LayoutKind.Sequential)]
    public struct MrbValue
    {
        MrbValueUnion value;
        public MrbVtype TT;

        public bool IsNil => TT == MrbVtype.MRB_TT_FALSE && value.I == 0;
        public long IntValue => value.I;
        public double FlaotValue => value.F;

        public unsafe string ToString(MRubyContext context)
        {
            var nstring = NativeMethods.MrbToString(context.DangerousGetPtr(), this);
            return System.Text.Encoding.UTF8.GetString(nstring.Bytes, nstring.Length);
        }

        public unsafe FixedUtf8String ToFixedUtf8String(MRubyContext context)
        {
            var nstring = NativeMethods.MrbToString(context.DangerousGetPtr(), this);
            return new FixedUtf8String(nstring.Bytes, nstring.Length);
        }
    }

    // NOTE: Assuming MRUBY_BOXING_NO
    // ReSharper disable InconsistentNaming
    public enum MrbVtype
    {
        MRB_TT_FALSE,
        MRB_TT_TRUE,
        MRB_TT_SYMBOL,
        MRB_TT_UNDEF,
        MRB_TT_FREE,
        MRB_TT_FLOAT,
        MRB_TT_INTEGER,
        MRB_TT_CPTR,
        MRB_TT_OBJECT,
        MRB_TT_CLASS,
        MRB_TT_MODULE,
        MRB_TT_ICLASS,
        MRB_TT_SCLASS,
        MRB_TT_PROC,
        MRB_TT_ARRAY,
        MRB_TT_HASH,
        MRB_TT_STRING,
        MRB_TT_RANGE,
        MRB_TT_EXCEPTION,
        MRB_TT_ENV,
        MRB_TT_CDATA,
        MRB_TT_FIBER,
        MRB_TT_STRUCT,
        MRB_TT_ISTRUCT,
        MRB_TT_BREAK,
        MRB_TT_COMPLEX,
        MRB_TT_RATIONAL,
        MRB_TT_BIGINT,
    }
    // ReShaper enable InconsistentNaming

#pragma warning disable CS8500
#pragma warning disable CS8981
    static unsafe class NativeMethods
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
        const string __DllName = "__Internal";
#else
        const string __DllName = "VitalRouter.MRuby.Native";
#endif
        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_allocf_set", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbAllocfSet(MrbAllocF allocF);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_ctx_new", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbContextCore* MrbContextNew();

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_ctx_dispose", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbContextDispose(MrbContextCore* state);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_callbacks_set", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbCallbacksSet(MrbContextCore* context, MrbCommandHandler onCommand, MrbErrorHandler onError);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_int32", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetInt32(MrbContextCore* state, byte* key, int value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_float", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetFloat(MrbContextCore* state, byte* key, float value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_bool", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetBool(MrbContextCore* state, byte* key, bool value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetString(MrbContextCore* state, byte* key, byte* value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_remove", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateRemove(MrbContextCore* state, byte* key);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_clear", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateClear(MrbContextCore* state);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_load", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbValue MrbLoad(MrbContextCore* state, MrbNString nString);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_value_release", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbValueRelease(MrbContextCore* state, MrbValue value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_compile", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbScriptCore* ScriptCompile(MrbContextCore* state, MrbNString nString);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_dispose", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbScriptDispose(MrbContextCore* context, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_status", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptStatus(MrbContextCore* context, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_start", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptStart(MrbContextCore* state, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_resume", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptResume(MrbContextCore* state, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_array_len", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern nint MrbArrayLen(MrbValue array);

        [DllImport(__DllName, EntryPoint = "mrb_ary_entry", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbValue MrbArrayEntry(MrbValue array, nint index);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_hash_len", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern nint MrbHashLen(MrbContextCore *ctx, MrbValue array);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_hash_keys", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbValue MrbHashKeys(MrbContextCore *ctx, MrbValue hash);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_hash_get", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbValue MrbHashGet(MrbContextCore *ctx, MrbValue hash, MrbValue key);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_to_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbNString MrbToString(MrbContextCore *ctx, MrbValue value);
    }
}