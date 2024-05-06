using System;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains some <see cref="IConfigKeyDefault{T}"/> presets.
    /// </summary>
    public static class ConfigKeyDefault
    {
        /// <summary>
        /// Uses the provided <see langword="unmanaged"/> value as a default.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="value">The constant value to use as a default.</param>
        /// <returns>A new default component.</returns>
        public static IConfigKeyDefault<T> Constant<T>(T value) where T : unmanaged
            => new ConfigKeyDefault<T>(() => value);
    }

    /// <summary>
    /// Represents a default value for an <see cref="IDefiningConfigKey{T}"/> using a factory function.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public sealed class ConfigKeyDefault<T> : IConfigKeyDefault<T>
    {
        private readonly Func<T> _getDefault;

        /// <summary>
        /// Creates a new default value on demand using the provided factory function.
        /// </summary>
        /// <param name="getDefault">The factory function that creates a new default value.</param>
        public ConfigKeyDefault(Func<T> getDefault)
        {
            _getDefault = getDefault;
        }

        /// <inheritdoc/>
        public T GetDefault() => _getDefault();

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> config)
        { }
    }

    /// <summary>
    /// Default value for a <see cref="IDefiningConfigKey"/>.
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