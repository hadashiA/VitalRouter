using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using MessagePack;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace VitalRouter.MRuby
{
    public class MRubyScriptException : Exception
    {
        public MRubyScriptException(string message) : base(message)
        {
        }
    }

    public enum MRubyScriptStatus
    {
        Created,
        Running,
        Resumed,
        Suspended,
        Transferred,
        Terminated,
    }

    static class SystemCommandNames
    {
        public static FixedUtf8String Log = new("vitalrouter:log");
        public static FixedUtf8String Wait = new("vitalrouter:wait");
    }

    public class MRubyScript : SafeHandle
    {
        public override bool IsInvalid => handle == IntPtr.Zero;
        public unsafe int ScriptId => DangerousGetPtr()->Id;

        internal static readonly Dictionary<int, MRubyScript> Scripts = new();

        TaskCompletionSource<bool>? completionSource;

        public unsafe MRubyScriptStatus Status
        {
            get
            {
                var status = NativeMethods.MrbScriptStatus(Context.DangerousGetPtr(), DangerousGetPtr());
                // enum mrb_fiber_state {
                //     MRB_FIBER_CREATED = 0,
                //     MRB_FIBER_RUNNING,
                //     MRB_FIBER_RESUMED,
                //     MRB_FIBER_SUSPENDED,
                //     MRB_FIBER_TRANSFERRED,
                //     MRB_FIBER_TERMINATED,
                // };
                return status switch
                {
                    0 => MRubyScriptStatus.Created,
                    1 => MRubyScriptStatus.Running,
                    2 => MRubyScriptStatus.Resumed,
                    3 => MRubyScriptStatus.Suspended,
                    4 => MRubyScriptStatus.Transferred,
                    _ => MRubyScriptStatus.Terminated,
                };
            }
        }

        public MRubyContext Context { get;  }

#pragma warning disable CS8500
#pragma warning disable CS8981
        unsafe MrbContextCore* DangerousGetStatePtr() => Context.DangerousGetPtr();
        unsafe MrbScriptCore* DangerousGetPtr() => (MrbScriptCore*)DangerousGetHandle();

        internal unsafe MRubyScript(MRubyContext context, MrbScriptCore* ptr) : base((IntPtr)ptr, true)
        {
            Context = context;
        }

#pragma warning restore CS8500
#pragma warning restore CS8981

        protected override unsafe bool ReleaseHandle()
        {
            NativeMethods.MrbScriptDispose(DangerousGetStatePtr(), DangerousGetPtr());
            return true;
        }

        public unsafe Task RunAsync(CancellationToken cancellation = default)
        {
            if (Status != MRubyScriptStatus.Created && Status != MRubyScriptStatus.Terminated)
            {
                throw new MRubyScriptException($"Script already running. {Status}");
            }

            completionSource = new TaskCompletionSource<bool>();
            if (cancellation.IsCancellationRequested)
            {
                completionSource.TrySetCanceled(cancellation);
            }

            var result = (NativeMethodResult)NativeMethods.MrbScriptStart(DangerousGetStatePtr(), DangerousGetPtr());
            if (cancellation.IsCancellationRequested)
            {
                completionSource.TrySetCanceled(cancellation);
            }

            switch (result)
            {
                case NativeMethodResult.Error:
                    completionSource.TrySetException(new MRubyScriptException("Failed to start script."));
                    break;
                case NativeMethodResult.Continue:
                    break;
                case NativeMethodResult.Done:
                    completionSource.TrySetResult(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return completionSource.Task;
        }

        public unsafe void Resume()
        {
            var result = (NativeMethodResult)NativeMethods.MrbScriptResume(DangerousGetStatePtr(), DangerousGetPtr());
            switch (result)
            {
                case NativeMethodResult.Error:
                    completionSource?.TrySetException(new MRubyScriptException("Unknown error"));
                    break;
                case NativeMethodResult.Continue:
                    break;
                case NativeMethodResult.Done:
                    completionSource?.TrySetResult(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Fail(Exception exception)
        {
            completionSource?.TrySetException(exception);
        }

        [MonoPInvokeCallback(typeof(MrbCommandHandler))]
        internal static unsafe void OnCommandCalled(int scriptId, byte* commandNamePtr, int commandNameLength, byte* payloadPtr, int payloadLength)
        {
            if (Scripts.TryGetValue(scriptId, out var script))
            {
                try
                {
                    var commandName = new FixedUtf8String(commandNamePtr, commandNameLength);
                    var payload = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(payloadPtr, payloadLength, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref payload, AtomicSafetyHandle.GetTempMemoryHandle());
#endif

                    if (SystemCommandNames.Log.Equals(commandName))
                    {
                        MRubyContext.GlobalLogHandler.Invoke(System.Text.Encoding.UTF8.GetString(payload));
                    }
                    else if (SystemCommandNames.Wait.Equals(commandName))
                    {
                        // var duration = MessagePackSerializer.Deserialize<float>(payload.AsMemory());
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _ = script.Context.CommandPreset.CommandCallFromMrubyAsync(script, commandName, payload);
                    }
                }
                catch (Exception ex)
                {
                    MRubyContext.GlobalErrorHandler.Invoke(ex);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        [MonoPInvokeCallback(typeof(MrbErrorHandler))]
        internal static unsafe void OnError(int scriptId, byte* exceptionInspection)
        {
            var message = new string((sbyte*)exceptionInspection);
            var ex = new MRubyScriptException(message);
            if (Scripts.TryGetValue(scriptId, out var script))
            {
                script.completionSource?.TrySetException(ex);
            }
            else
            {
                MRubyContext.GlobalErrorHandler.Invoke(ex);
            }
        }
    }
}