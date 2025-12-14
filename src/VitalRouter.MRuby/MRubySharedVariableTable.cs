using System.Collections.Generic;
using MRubyCS;
using MRubyCS.Serializer;

namespace VitalRouter.MRuby
{
    public class MRubySharedVariableTable
    {
        readonly Dictionary<Symbol, MRubyValue> serializedValues = new();
        readonly MRubyState state;

        public MRubySharedVariableTable(MRubyState state)
        {
            this.state = state;
        }

        public Dictionary<Symbol, MRubyValue>.KeyCollection Keys => serializedValues.Keys;

        public bool HasKey(string key) => HasKey(state.Intern(key));
        public bool HasKey(Symbol key) => serializedValues.ContainsKey(key);

        public MRubyValue GetAsMRubyValue(string key) => GetAsMRubyValue(state.Intern(key));
        public MRubyValue GetAsMRubyValue(Symbol key) => serializedValues.GetValueOrDefault(key);

        public T? GetOrDefault<T>(string key) =>
            GetOrDefault<T>(state.Intern(key));

        public T? GetOrDefault<T>(Symbol key)
        {
            if (TryGet<T>(key, out var value))
            {
                return value;
            }
            return default;
        }

        public bool TryGet<T>(string key, out T value) =>
            TryGet(state.Intern(key), out value);

        public bool TryGet<T>(Symbol key, out T value)
        {
            if (serializedValues.TryGetValue(key, out var serializedValue))
            {
                value = MRubyValueSerializer.Deserialize<T>(serializedValue, state)!;
                return true;
            }
            value = default!;
            return false;
        }

        public void Set(string key, MRubyValue value) => Set(state.Intern(key), value);
        public void Set(Symbol key, MRubyValue value) => serializedValues[key] = value;

        public void Set<T>(string key, T value) => Set(state.Intern(key), value);

        public void Set<T>(Symbol key, T value)
        {
            var mrubyValue = MRubyValueSerializer.Serialize(value, state);
            serializedValues[key] = mrubyValue;
        }

        public bool Remove(string key) => Remove(state.Intern(key));
        public bool Remove(Symbol key) => serializedValues.Remove(key);
        public void Clear() => serializedValues.Clear();
    }
}
