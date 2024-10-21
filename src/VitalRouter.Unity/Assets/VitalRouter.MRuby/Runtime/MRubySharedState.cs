using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public unsafe class MRubySharedState
    {
        readonly MRubyContext context;
        readonly ConcurrentDictionary<string, object> values = new();

        public MRubySharedState(MRubyContext context)
        {
            this.context = context;
        }

        public IEnumerable<string> Keys() => values.Keys;

        public void Remove(string key)
        {
            var keyMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(key.Length + 1);
            var keyUtf8 = keyMaxBytes > 255 ? new byte[keyMaxBytes] : stackalloc byte[keyMaxBytes];
            var keyBytesWritten = System.Text.Encoding.UTF8.GetBytes(key, keyUtf8);

            fixed (byte* keyPtr = keyUtf8)
            {
                NativeMethods.MrbStateRemove(context.DangerousGetPtr(), new MrbNString
                {
                    Bytes = keyPtr,
                    Length = keyBytesWritten
                });
            }
            values.TryRemove(key, out _);
        }

        public void Clear()
        {
            NativeMethods.MrbStateClear(context.DangerousGetPtr());
            values.Clear();
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

        public void Set(string key, int value, bool asSymbol = false)
        {
            Set(typeof(int), key, value, asSymbol);
        }

        public void Set(string key, float value, bool asSymbol = false)
        {
            Set(typeof(float), key, value, asSymbol);
        }

        public void Set(string key, bool value, bool asSymbol = false)
        {
            Set(typeof(bool), key, value, asSymbol);
        }

        public void Set(string key, string value, bool asSymbol = false)
        {
            Set(typeof(string), key, value, asSymbol);
        }

        void Set(Type type, string key, object boxedValue, bool asSymbol)
        {
            var keyMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(key.Length + 1);
            var keyUtf8 = keyMaxBytes > 255 ? new byte[keyMaxBytes] : stackalloc byte[keyMaxBytes];
            var keyBytesWritten = System.Text.Encoding.UTF8.GetBytes(key, keyUtf8);

            var typeCode = Type.GetTypeCode(type);
            fixed (byte* keyPtr = keyUtf8)
            {
                var keyNString = new MrbNString { Bytes = keyPtr, Length = keyBytesWritten };
                switch (typeCode)
                {
                    case TypeCode.Boolean:
                        NativeMethods.MrbStateSetBool(context.DangerousGetPtr(), keyNString, (bool)boxedValue);
                        break;
                    case TypeCode.Int32:
                        NativeMethods.MrbStateSetInt32(context.DangerousGetPtr(), keyNString, (int)boxedValue);
                        break;
                    case TypeCode.Single:
                        NativeMethods.MrbStateSetFloat(context.DangerousGetPtr(), keyNString, (float)boxedValue);
                        break;
                    case TypeCode.String:
                    {
                        var value = (string)boxedValue;
                        var valueMaxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(value.Length + 1);
                        var valueUtf8 = valueMaxBytes > 255
                            ? new byte[valueMaxBytes]
                            : stackalloc byte[valueMaxBytes];
                        var valueBytesWritten = System.Text.Encoding.UTF8.GetBytes(value, valueUtf8);

                        fixed (byte* valuePtr = valueUtf8)
                        {
                            var valueNString = new MrbNString { Bytes = valuePtr, Length = valueBytesWritten };
                            if (asSymbol)
                            {
                                NativeMethods.MrbStateSetSymbol(context.DangerousGetPtr(), keyNString, valueNString);
                            }
                            else
                            {
                                NativeMethods.MrbStateSetString(context.DangerousGetPtr(), keyNString, valueNString);
                            }
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
