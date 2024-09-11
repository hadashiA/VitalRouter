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
    unsafe delegate void MrbCommandHandler(
        int scriptId,
        byte *commandName,
        int commandNameLength,
        byte *payload,
        int payloadLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void MrbErrorHandler(int scriptId, byte* commandName);

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
    unsafe struct MrbSource
    {
        public byte* Bytes;
        public int Length;
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
        public static extern void MrbLoad(MrbContextCore* state, MrbSource source);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_compile", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern MrbScriptCore* ScriptCompile(MrbContextCore* state, MrbSource source);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_dispose", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MrbScriptDispose(MrbContextCore* context, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_status", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptStatus(MrbContextCore* context, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_start", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptStart(MrbContextCore* state, MrbScriptCore* script);

        [DllImport(__DllName, EntryPoint = "vitalrouter_mrb_script_resume", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int MrbScriptResume(MrbContextCore* state, MrbScriptCore* script);
    }
}
