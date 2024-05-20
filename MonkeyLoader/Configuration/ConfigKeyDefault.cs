using System;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Implements a components that provide a default value for
    /// <see cref="IDefiningConfigKey{T}"/>s by using a provided factory function to generate the value.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public sealed class ConfigKeyDefault<T> : IConfigKeyDefault<T>
    {
        private readonly Func<T> _makeValue;

        /// <summary>
        /// Creates an <see cref="IConfigKeyDefault{T}"/> component that uses
        /// the provided factory function to generate the default value.
        /// </summary>
        /// <param name="makeValue">The factory function that creates a new default value.</param>
        public ConfigKeyDefault(Func<T> makeValue)
        {
            _makeValue = makeValue;
        }

        /// <inheritdoc/>
        public T GetDefault() => _makeValue();

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> config)
        { }
    }

    /// <summary>
    /// Implements a component that provides a default value for
    /// <see cref="IDefiningConfigKey{T}"/>s by using a provided <see langword="unmanaged"/> value.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public sealed class ConfigKeyDefaultConstant<T> : IConfigKeyDefault<T>
        where T : unmanaged
    {
        private readonly T _value;

        /// <summary>
        /// Creates an <see cref="IConfigKeyDefault{T}"/> component that uses
        /// the provided <see langword="unmanaged"/> value as the default value.
        /// </summary>
        /// <param name="value">The constant value to use as a default.</param>
        public ConfigKeyDefaultConstant(T value)
        {
            _value = value;
        }

        /// <inheritdoc/>
        public T GetDefault() => _value;

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> entity)
        { }
    }

    /// <summary>
    /// Defines the interface for components that provide a default value for <see cref="IDefiningConfigKey{T}"/>s.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public interface IConfigKeyDefault<T> : IConfigKeyComponent<IDefiningConfigKey<T>>
    {
        /// <summary>
        /// Creates a new instance of the default value for the inner type <typeparamref name="T"/> of the config key.
        /// </summary>
        /// <returns>The new default value.</returns>
        public T GetDefault();
    }
}