using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Base class for untyped config key wrappers.
    /// </summary>
    public abstract class ConfigKeyWrapper : ConfigKeyWrapperBase<IConfigKey>
    {
        /// <inheritdoc/>
        protected ConfigKeyWrapper(IConfigKey key) : base(key)
        { }
    }

    /// <summary>
    /// Base class for typed config key wrappers.
    /// </summary>
    /// <typeparam name="T">The typeparameter of the wrapped <see cref="ITypedConfigKey{T}"/>.</typeparam>
    public abstract class ConfigKeyWrapper<T> : ConfigKeyWrapperBase<ITypedConfigKey<T>>, ITypedConfigKey<T>
    {
        /// <inheritdoc/>
        public Type ValueType => Key.ValueType;

        /// <summary>
        /// Wraps the given typed config key.
        /// </summary>
        /// <param name="typedKey">The typed key to wrap.</param>
        protected ConfigKeyWrapper(ITypedConfigKey<T> typedKey) : base(typedKey)
        { }
    }

    /// <summary>
    /// Base class for any config key wrappers.
    /// </summary>
    /// <typeparam name="TKey">The type of the wrapped key.</typeparam>
    public abstract class ConfigKeyWrapperBase<TKey> : IConfigKeyWrapper<TKey>
        where TKey : IConfigKey
    {
        /// <inheritdoc/>
        IConfigKey IConfigKey.AsUntyped => Key.AsUntyped;

        /// <inheritdoc/>
        public bool IsDefiningKey => Key.IsDefiningKey;

        /// <inheritdoc/>
        public TKey Key { get; }

        IConfigKey IConfigKeyWrapper.Key => Key;

        /// <inheritdoc/>
        public string Name => Key.Name;

        /// <summary>
        /// Wraps the given config key.
        /// </summary>
        /// <param name="key">The key to wrap.</param>
        protected ConfigKeyWrapperBase(TKey key)
        {
            Key = key;
        }

        /// <inheritdoc/>
        public bool Equals(IConfigKey other) => Key.Equals(other);
    }

    /// <summary>
    /// Defines the interface for config key wrappers with a specific <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the wrapped key.</typeparam>
    public interface IConfigKeyWrapper<TKey> : IConfigKeyWrapper where TKey : IConfigKey
    {
        /// <summary>
        /// Gets the concrete wrapped config key.
        /// </summary>
        public new TKey Key { get; }
    }

    /// <summary>
    /// Defines the interface for any <see cref="IConfigKey"/> wrappers.
    /// </summary>
    public interface IConfigKeyWrapper : IConfigKey
    {
        /// <summary>
        /// Gets the unspecific wrapped config key.
        /// </summary>
        public IConfigKey Key { get; }
    }
}