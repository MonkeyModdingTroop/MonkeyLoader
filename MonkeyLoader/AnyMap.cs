using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Represents a type safe dictionary of Types to objects of the type.
    /// </summary>
    public sealed class AnyMap
    {
        private readonly Dictionary<Type, object?> _dict = new();

        public IEnumerable<Type> Keys => _dict.Keys;

        public void Add<T>(T value) => _dict.Add(typeof(T), value);

        public void Clear() => _dict.Clear();

        public bool ContainsKey<T>() => _dict.ContainsKey(typeof(T));

        public IEnumerable<T> GetCastableValues<T>()
            => _dict.Values.SelectCastable<object, T>();

        public T GetOrCreateValue<T>() where T : new()
        {
            if (TryGetValue<T>(out var value))
                return value!;

            value = new T();
            Add(value);

            return value;
        }

        public T GetOrCreateValue<T>(Func<T> valueFactory)
        {
            if (TryGetValue<T>(out var value))
                return value!;

            value = valueFactory();
            Add(value);

            return value;
        }

        public T GetValue<T>() => (T)_dict[typeof(T)]!;

        public void Remove<T>() => _dict.Remove(typeof(T));

        public bool TryGetValue<T>(out T? value)
        {
            if (_dict.TryGetValue(typeof(T), out var obj))
            {
                value = (T)obj!;
                return true;
            }

            value = default;
            return false;
        }
    }
}