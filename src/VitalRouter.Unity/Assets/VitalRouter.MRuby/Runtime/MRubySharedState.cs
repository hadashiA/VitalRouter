using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public class MRubySharedState
    {
        readonly MRubyContext context;
        readonly ConcurrentDictionary<string, object> values = new();

        public MRubySharedState(MRubyContext context)
        {
            this.context = context;
        }

        public unsafe void Remove(string key)
        {
            var keyMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(key.Length + 1);
            Span<byte> keyUtf8 = keyMaxBytes > 255
                ? new byte[keyMaxBytes]
                : stackalloc byte[keyMaxBytes];
            var keyBytesWritten = System.Text.Encoding.UTF8.GetBytes(key, keyUtf8);
            keyUtf8[keyBytesWritten] = 0; // NULL terminated

            fixed (byte* keyPtr = keyUtf8)
            {
                NativeMethods.MrbStateRemove(context.DangerousGetPtr(), keyPtr);
            }
        }

        public unsafe void Clear()
        {
            NativeMethods.MrbStateClear(context.DangerousGetPtr());
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (values.TryGetValue(key, out var valueObj) && valueObj is T v)
            {
                value = v;
                return true;
            }
            value = default!;
            return false;
        }

        public T Get<T>(string key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Key not found: {key}");
        }

        public T GetOrDefault<T>(string key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return default!;
        }

        public void Set(string key, int value)
        {
            Set(typeof(int), key, value);
        }

        public void Set(string key, float value)
        {
            Set(typeof(float), key, value);
        }

        public void Set(string key, bool value)
        {
            Set(typeof(bool), key, value);
        }

        public void Set(string key, string value)
        {
            Set(typeof(string), key, value);
        }

        unsafe void Set(Type type, string key, object boxedValue)
        {
            var keyMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(key.Length + 1);
            Span<byte> keyUtf8 = keyMaxBytes > 255
                ? new byte[keyMaxBytes]
                : stackalloc byte[keyMaxBytes];
            var keyBytesWritten = System.Text.Encoding.UTF8.GetBytes(key, keyUtf8);
            keyUtf8[keyBytesWritten] = 0; // NULL terminated

            var typeCode = Type.GetTypeCode(type);
            fixed (byte* keyPtr = keyUtf8)
            {
                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        NativeMethods.MrbStateSetBool(context.DangerousGetPtr(), keyPtr, (bool)boxedValue);
                        break;
                    case TypeCode.Int32:
                        NativeMethods.MrbStateSetInt32(context.DangerousGetPtr(), keyPtr, (int)boxedValue);
                        break;
                    case TypeCode.Single:
                        NativeMethods.MrbStateSetFloat(context.DangerousGetPtr(), keyPtr, (float)boxedValue);
                        break;
                    case TypeCode.String:
                    {
                        var value = (string)boxedValue;
                        var valueMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(value.Length + 1);
                        Span<byte> valueUtf8 = valueMaxBytes > 255
                            ? new byte[valueMaxBytes]
                            : stackalloc byte[valueMaxBytes];
                        var valueBytesWritten = System.Text.Encoding.UTF8.GetBytes(value, valueUtf8);
                        valueUtf8[valueBytesWritten] = 0; // NULL terminated
                        fixed (byte* valuePtr = valueUtf8)
                        {
                            NativeMethods.MrbStateSetString(context.DangerousGetPtr(), keyPtr, valuePtr);
                        }
                        break;
                    }
                    default:
                        throw new NotSupportedException(typeCode.ToString());
                }
            }
            values.AddOrUpdate(key, boxedValue, (_, v) => v);
        }
    }
}
