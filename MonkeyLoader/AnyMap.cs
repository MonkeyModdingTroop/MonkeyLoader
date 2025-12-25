using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Represents a type safe dictionary mapping <see cref="Type"/>s to objects of the type.
    /// </summary>
    public sealed class AnyMap
    {
        private readonly Dictionary<Type, object?> _dict = [];

        /// <summary>
        /// Gets all <see cref="Type"/>s that have a set value in this AnyMap.
        /// </summary>
        public IEnumerable<Type> Keys => _dict.Keys;

        /// <summary>
        /// Adds the given value for type <typeparamref name="T"/> to this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to associate with the type.</param>
        public void Add<T>(T value) => _dict.Add(typeof(T), value);

        /// <summary>
        /// Removes all keys and values of this AnyMap.
        /// </summary>
        public void Clear() => _dict.Clear();

        /// <summary>
        /// Determines whether this AnyMap contains a value for type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns><c>true</c> if there is a value for the type; otherwise, <c>false</c>.</returns>
        public bool ContainsKey<T>() => _dict.ContainsKey(typeof(T));

        /// <summary>
        /// Gets all non-<c>null</c> values that are castable to <typeparamref name="T"/> in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The common type of the values.</typeparam>
        /// <returns>All non-<c>null</c> values that are castable to <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetCastableValues<T>()
            => _dict.Values.OfType<T>();

        /// <summary>
        /// Gets the value associated with the type <typeparamref name="T"/> in this AnyMap,
        /// or creates and associates a new instance of <typeparamref name="T"/> using its parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>The value associated with the type or the newly created instance.</returns>
        public T GetOrCreateValue<T>() where T : new()
        {
            if (TryGetValue<T>(out var value))
                return value!;

            value = new T();
            Add(value);

            return value;
        }

        /// <summary>
        /// Gets the value associated with the type <typeparamref name="T"/> in this AnyMap,
        /// or creates and associates the value returned by the given <paramref name="valueFactory"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="valueFactory">A factory method that creates a value when there is none yet.</param>
        /// <returns>The value associated with the type or the newly created instance.</returns>
        public T GetOrCreateValue<T>(Func<T> valueFactory)
        {
            if (TryGetValue<T>(out var value))
                return value!;

            value = valueFactory();
            Add(value);

            return value;
        }

        /// <summary>
        /// Gets the value associated with the type <typeparamref name="T"/> in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>The value associated with the type.</returns>
        /// <exception cref="KeyNotFoundException">When there is no value for <typeparamref name="T"/>.</exception>
        public T GetValue<T>() => (T)_dict[typeof(T)]!;

        /// <summary>
        /// Gets the value associated with the given type in this AnyMap.
        /// </summary>
        /// <returns>The value associated with the given type.</returns>
        /// <exception cref="KeyNotFoundException">When there is no value for the <paramref name="type"/>.</exception>
        public object GetValue(Type type) => _dict[type]!;

        /// <summary>
        /// Gets the value associated with the given type in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value. Only used for casting, not access.</typeparam>
        /// <returns>The value associated with the given type.</returns>
        /// <exception cref="KeyNotFoundException">When there is no value for the <paramref name="type"/>.</exception>
        public T GetValue<T>(Type type) => (T)GetValue(type);

        /// <summary>
        /// Removes the value associated with the type <typeparamref name="T"/> in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void Remove<T>() => _dict.Remove(typeof(T));

        /// <summary>
        /// Removes the value associated with the given type in this AnyMap.
        /// </summary>
        /// <param name="type">The type of the value.</param>
        public void Remove(Type type) => _dict.Remove(type);

        /// <summary>
        /// Sets the given value for type <typeparamref name="T"/> in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The new value to associate with the type.</param>
        public void SetValue<T>(T value) => _dict[typeof(T)] = value;

        /// <summary>
        /// Sets the given value for the given type in this AnyMap.
        /// </summary>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The new value to associate with the type.</param>
        /// <exception cref="InvalidCastException">When the given value is not assignable to the given type.</exception>
        public void SetValue(Type type, object? value)
        {
            if (value is not null && !type.IsAssignableFrom(value.GetType()))
                throw new InvalidCastException("The given value is not valid for given type!");

            _dict[type] = value;
        }

        /// <summary>
        /// Tries to get the value associated with the given type in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value. Only used for casting, not access.</typeparam>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The value if it was found; otherwise, <c>default(<typeparamref name="T"/>)</c>.</param>
        /// <returns><c>true</c> if the value was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(Type type, out T? value)
        {
            if (_dict.TryGetValue(type, out var obj))
            {
                value = (T)obj!;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get the value associated with the given type in this AnyMap.
        /// </summary>
        /// <param name="type">The type of the value.</param>
        /// <param name="value">The value if it was found; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the value was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(Type type, out object? value)
            => TryGetValue<object>(type, out value);

        /// <summary>
        /// Tries to get the value associated with the type <typeparamref name="T"/> in this AnyMap.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value if it was found; otherwise, <c>default(<typeparamref name="T"/>)</c>.</param>
        /// <returns><c>true</c> if the value was found; otherwise, <c>false</c>.</returns>
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