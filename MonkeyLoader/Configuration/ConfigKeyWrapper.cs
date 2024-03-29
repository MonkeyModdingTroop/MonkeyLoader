using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Base class for config key wrappers.
    /// </summary>
    public abstract class ConfigKeyWrapper<TKey> : IConfigKey
        where TKey : IConfigKey
    {
        /// <inheritdoc/>
        public bool IsDefiningKey => Key.IsDefiningKey;

        /// <summary>
        /// Gets the wrapped config key.
        /// </summary>
        public TKey Key { get; }

        /// <inheritdoc/>
        public string Name => Key.Name;

        /// <summary>
        /// Wraps the given config key.
        /// </summary>
        /// <param name="key">The key to wrap.</param>
        protected ConfigKeyWrapper(TKey key)
        {
            Key = key;
        }

        /// <inheritdoc/>
        public bool Equals(IConfigKey other) => Key.Equals(other);
    }

    /// <summary>
    /// Base class for typed config key wrappers.
    /// </summary>
    public abstract class TypedConfigKeyWrapper<TValue> : ConfigKeyWrapper<ITypedConfigKey<TValue>>, ITypedConfigKey<TValue>
    {
        /// <inheritdoc/>
        public Type ValueType => Key.ValueType;

        /// <summary>
        /// Wraps the given typed config key.
        /// </summary>
        /// <param name="typedKey">The typed key to wrap.</param>
        protected TypedConfigKeyWrapper(ITypedConfigKey<TValue> typedKey) : base(typedKey)
        { }
    }
}