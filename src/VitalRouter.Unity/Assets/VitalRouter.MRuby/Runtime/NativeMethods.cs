using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

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

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct RBasic
    {
        IntPtr c;
        IntPtr gcnext;
        public MrbVtype TT;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct RInteger
    {
        IntPtr c;
        IntPtr gcnext;
        public MrbVtype TT;
        fixed byte Footer[3];
        public nint IntValue;
    }

    // mruby types

    // ReSharper disable InconsistentNaming
    public enum MrbVtype : byte
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

    // NOTE: Assuming MRB_WORD_BOXING
    //
    // mrb_value representation:
    //
    // 64-bit word with inline float:
    //   nil   : ...0000 0000 (all bits are 0)
    //   false : ...0000 0100 (mrb_fixnum(v) != 0)
    //   true  : ...0000 1100
    //   undef : ...0001 0100
    //   symbol: ...SSS1 1100 (use only upper 32-bit as symbol value with MRB_64BIT)
    //   fixnum: ...IIII III1
    //   float : ...FFFF FF10 (51 bit significands; require MRB_64BIT)
    //   object: ...PPPP P000
    //
    // 32-bit word with inline float:
    //   nil   : ...0000 0000 (all bits are 0)
    //   false : ...0000 0100 (mrb_fixnum(v) != 0)
    //   true  : ...0000 1100
    //   undef : ...0001 0100
    //   symbol: ...SSS1 0100 (symbol occupies 20bits)
    //   fixnum: ...IIII III1
    //   float : ...FFFF FF10 (22 bit significands; require MRB_64BIT)
    //   object: ...PPPP P000
    //
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct MrbValue
    {
        [FieldOffset(0)]
        nint bits;

        [FieldOffset(0)]
        RBasic* ptr;

        public MrbVtype TT => this switch
        {
            { IsFalse: true } => MrbVtype.MRB_TT_FALSE,
            { IsTrue: true } => MrbVtype.MRB_TT_TRUE,
            { IsUndef: true } => MrbVtype.MRB_TT_UNDEF,
            { IsSymbol: true } => MrbVtype.MRB_TT_SYMBOL,
            { IsFixnum: true } => MrbVtype.MRB_TT_INTEGER,
            { IsFloat: true } => MrbVtype.MRB_TT_FLOAT,
            { IsObject: true} => ptr->TT,
            _ => default
        };

        public bool IsNil => bits == 0;
        public bool IsFalse => bits == 0b0000_0100;
        public bool IsTrue => bits == 0b0000_1100;
        public bool IsUndef => bits == 0b0001_0100;
        public bool IsSymbol => (bits & 0b1_1111) == 0b1_1100;
        public bool IsFixnum => (bits & 1) == 1;
        public bool IsFloat => (bits & 0b11) == 0b10;
        public bool IsObject => (bits & 0b111) == 0;

        public long IntValue
        {
            get
            {
                if (IsObject && ptr->TT == MrbVtype.MRB_TT_INTEGER)
                {
                    return ((RInteger*)ptr)->IntValue;
                }
                return bits >> 1;
            }
        }

        public double FloatValue
        {
            get
            {
                // Assume that MRB_USE_FLOAT32 is not defined
                // Assume that MRB_WORDBOX_NO_FLOAT_TRUNCATE is not defined
                var fbits = (bits & ~3) | 2;
                return UnsafeUtility.As<nint, double>(ref fbits);
            }
        }

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
        public static extern void MrbStateSetInt32(MrbContextCore* state, MrbNString key, int value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_float", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetFloat(MrbContextCore* state, MrbNString key, float value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_bool", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetBool(MrbContextCore* state, MrbNString key, bool value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetString(MrbContextCore* state, MrbNString key, MrbNString value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_set_symbol", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateSetSymbol(MrbContextCore* state, MrbNString key, MrbNString value);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_state_remove", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbStateRemove(MrbContextCore* state, MrbNString key);

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