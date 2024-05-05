using System;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Default value for a <see cref="IDefiningConfigKey"/>.
    /// </summary>
    /// <typeparam name="T">Inner value type of the config key.</typeparam>
    public sealed class ConfigKeyDefault<T> : IConfigKeyDefault<T>
    {
        private readonly Func<T> _getDefault;

        /// <summary>
        /// Creates a new default value on demand using the provided factory.
        /// </summary>
        /// <param name="getDefault">Factory function which creates a new default value.</param>
        public ConfigKeyDefault(Func<T> getDefault)
        {
            _getDefault = getDefault;
        }

        /// <summary>
        /// Uses the provided <see langword="unmanaged"/> value as a default.
        /// </summary>
        /// <typeparam name="TUnmanaged">Inner value type of the config key.</typeparam>
        /// <param name="value">Value to use as a default.</param>
        /// <returns></returns>
        public static ConfigKeyDefault<TUnmanaged> Const<TUnmanaged>(TUnmanaged value)
            where TUnmanaged : unmanaged
            => new(() => value);

        /// <inheritdoc/>
        public T GetDefault() => _getDefault();

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> config)
        { }
    }

    /// <summary>
    /// Default value for a <see cref="IDefiningConfigKey"/>.
    /// </summary>
    /// <typeparam name="T">Inner value type of the config key.</typeparam>
    public interface IConfigKeyDefault<T> : IConfigKeyComponent<IDefiningConfigKey<T>>
    {
        /// <summary>
        /// Creates a new instance of the default value for the inner type <typeparamref name="T"/> of the config key.
        /// </summary>
        /// <returns>The new default value.</returns>
        public T GetDefault();
    }
}