using System.Collections;
using System.Collections.Generic;

namespace VitalRouter.MRuby
{
    public static partial class MrbValueExtensions
    {
        public static int GetArrayLength(this MrbValue mrbValue)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_ARRAY)
            {
                throw new MRubySerializationException($"mrb_value is not an Array: {mrbValue.TT}");
            }
            return (int)NativeMethods.MrbArrayLen(mrbValue);
        }

        public static unsafe int GetHashLength(this MrbValue mrbValue, MRubyContext context)
        {
            if (mrbValue.TT != MrbVtype.MRB_TT_HASH)
            {
                throw new MRubySerializationException($"mrb_value is not an Hash: {mrbValue.TT}");
            }
            return (int)NativeMethods.MrbHashLen(context.DangerousGetPtr(), mrbValue);
        }

        public static ArrayEnumerator AsArrayEnumerable(this MrbValue mrbValue)
        {
            return new ArrayEnumerator(mrbValue);
        }

        public static HashEnumerator AsHashEnumerable(this MrbValue mrbValue, MRubyContext context)
        {
            return new HashEnumerator(mrbValue, context);
        }
    }

    public struct ArrayEnumerator : IEnumerator<MrbValue>, IEnumerable<MrbValue>
    {
        readonly MrbValue array;
        readonly int length;
        int index; // -1 = not started, -2 = ended/disposed
        MrbValue currentElement;

        public ArrayEnumerator(MrbValue array)
        {
            if (array.TT != MrbVtype.MRB_TT_ARRAY)
            {
                throw new MRubySerializationException($"The value is not an ARRAY. {array.TT}");
            }

            this.array = array;
            index = -1;
            length = (int)NativeMethods.MrbArrayLen(array);
            currentElement = default;
        }

        public bool MoveNext()
        {
            if (index == -2) return false;
            index++;

            if (index >= length)
            {
                index = -2;
                currentElement = default;
                return false;
            }

            currentElement = NativeMethods.MrbArrayEntry(array, index);
            return true;
        }

        public MrbValue Current => currentElement;
        object? IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            index = -1;
            currentElement = default;
        }

        public void Dispose()
        {
            index = -2;
            currentElement = default;
        }

        public IEnumerator<MrbValue> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }

    // TODO:
    public unsafe struct HashEnumerator : IEnumerator<KeyValuePair<MrbValue, MrbValue>>, IEnumerable<KeyValuePair<MrbValue, MrbValue>>
    {
        readonly MRubyContext context;
        readonly MrbValue hash;
        readonly MrbValue keys;
        readonly int length;
        int index; // -1 = not started, -2 = ended/disposed
        KeyValuePair<MrbValue, MrbValue> currentElement;

        public HashEnumerator(MrbValue hash, MRubyContext context)
        {
            if (hash.TT != MrbVtype.MRB_TT_HASH)
            {
                throw new MRubySerializationException($"The value is not an Hash. {hash.TT}");
            }

            this.context = context;
            this.hash = hash;
            keys = NativeMethods.MrbHashKeys(context.DangerousGetPtr(), hash);
            index = -1;
            length = (int)NativeMethods.MrbArrayLen(keys);
            currentElement = default;
        }

        public bool MoveNext()
        {
            if (index == -2) return false;
            index++;

            if (index >= length)
            {
                index = -2;
                currentElement = default;
                return false;
            }

            var key  = NativeMethods.MrbArrayEntry(keys, index);
            var value = NativeMethods.MrbHashGet(context.DangerousGetPtr(), hash, key);
            currentElement = new KeyValuePair<MrbValue, MrbValue>(key, value);
            return true;
        }

        public KeyValuePair<MrbValue, MrbValue> Current => currentElement;
        object? IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            index = -1;
            currentElement = default;
        }

        public void Dispose()
        {
            index = -2;
            currentElement = default;
        }

        public IEnumerator<KeyValuePair<MrbValue, MrbValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}