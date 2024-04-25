using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents a identifier-only config item, which can be used to
    /// get or set the values of defining keys with the same <see cref="Id">Id</see>.
    /// </summary>
    public class ConfigKey : IConfigKey
    {
        /// <summary>
        /// The event label used when a config item's value triggered
        /// an <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
        /// </summary>
        public const string CollectionChangedEventLabel = "CollectionChanged";

        /// <summary>
        /// The event label used when a config item's value triggered
        /// an <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        public const string PropertyChangedEventLabel = "PropertyChanged";

        /// <summary>
        /// The event label used when a config item's value is set from getting the computed default.
        /// </summary>
        public const string SetFromDefaultEventLabel = "Default";

        /// <summary>
        /// Gets the custom <see cref="IEqualityComparer{T}"/> for <see cref="IConfigKey"/>s.
        /// </summary>
        public static readonly IEqualityComparer<IConfigKey> EqualityComparer = new ConfigKeyEqualityComparer();

        IConfigKey IConfigKey.AsUntyped => this;

        /// <inheritdoc/>
        public string Id { get; }

        /// <inheritdoc/>
        public bool IsDefiningKey => false;

        /// <summary>
        /// Creates a new identifier-only config item with the given id.
        /// </summary>
        /// <param name="id">The mod-unique identifier of the config item. Must not be null or whitespace.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="id"/> is null or whitespace.</exception>
        public ConfigKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id), "Config key identifier must not be null or whitespace!");

            Id = id;
        }

        /// <summary>
        /// Creates a new <see cref="ConfigKey"/> instance from the given name.
        /// </summary>
        /// <param name="id">The mod-unique identifier of the config item.</param>
        public static implicit operator ConfigKey(string id) => new(id);

        /// <summary>
        /// Checks if two <see cref="ConfigKey"/>s are unequal.
        /// </summary>
        /// <param name="left">The first key.</param>
        /// <param name="right">The second key.</param>
        /// <returns><c>true</c> if they're considered unequal.</returns>
        public static bool operator !=(ConfigKey? left, ConfigKey? right)
            => !EqualityComparer.Equals(left!, right!);

        /// <summary>
        /// Checks if two <see cref="ConfigKey"/>s are equal.
        /// </summary>
        /// <param name="left">The first key.</param>
        /// <param name="right">The second key.</param>
        /// <returns><c>true</c> if they're considered equal.</returns>
        public static bool operator ==(ConfigKey? left, ConfigKey? right)
            => EqualityComparer.Equals(left!, right!);

        /// <summary>
        /// Checks if the given object can be considered equal to this one.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns><c>true</c> if the other object is considered equal.</returns>
        public override bool Equals(object? obj) => obj is IConfigKey otherKey && Equals(otherKey);

        /// <inheritdoc/>
        public bool Equals(IConfigKey? other) => EqualityComparer.Equals(this, other!);

        /// <inheritdoc/>
        public override int GetHashCode() => EqualityComparer.GetHashCode(this);

        /// <summary>
        /// <see cref="IEqualityComparer{T}"/> for <see cref="IConfigKey"/>s.
        /// </summary>
        private sealed class ConfigKeyEqualityComparer : IEqualityComparer<IConfigKey>
        {
            /// <inheritdoc/>
            public bool Equals(IConfigKey? x, IConfigKey? y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x is ITypedConfigKey typedX && y is ITypedConfigKey typedY)
                    return typedX.ValueType == typedY.ValueType && typedX.Id == typedY.Id;

                return x is not null && y is not null && x.Id == y.Id;
            }

            /// <inheritdoc/>
            public int GetHashCode(IConfigKey? obj)
            {
                if (obj is ITypedConfigKey typedKey)
                    return unchecked((31 * typedKey.ValueType.GetHashCode()) + obj.Id.GetHashCode());

                return obj?.Id.GetHashCode() ?? 0;
            }
        }
    }

    /// <inheritdoc/>
    /// <typeparam name="T">The type of this config item's value.</typeparam>
    public class ConfigKey<T> : ConfigKey, ITypedConfigKey<T>
    {
        /// <inheritdoc/>
        public IConfigKey AsUntyped { get; }

        IConfigKey IConfigKey.AsUntyped => AsUntyped;

        /// <inheritdoc/>
        public Type ValueType { get; } = typeof(T);

        /// <inheritdoc/>
        public ConfigKey(string id) : base(id)
        {
            AsUntyped = new ConfigKey(id);
        }

        /// <summary>
        /// Creates a new <see cref="ConfigKey{T}"/> instance from the given name.
        /// </summary>
        /// <param name="name">The mod-unique name of the config item.</param>
        public static implicit operator ConfigKey<T>(string name) => new(name);
    }

    /// <summary>
    /// Defines a name-only config item, which can be used to get or set the values of defining keys with the same name.
    /// </summary>
    public interface IConfigKey : IEquatable<IConfigKey>
    {
        /// <summary>
        /// Gets the untyped version of this config item.
        /// </summary>
        public IConfigKey AsUntyped { get; }

        /// <summary>
        /// Gets the mod-unique identifier of this config item.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets whether this instance defines the config item with this <see cref="Id">Name</see>.
        /// </summary>
        public bool IsDefiningKey { get; }
    }

    /// <summary>
    /// Defines a name-only typed config item, which can be used to get or set the values of defining keys with the same name.
    /// </summary>
    /// <remarks>
    /// Generally <see cref="ITypedConfigKey{T}"/> should be used instead, unless the generic type gets in the way.
    /// </remarks>
    public interface ITypedConfigKey : IConfigKey
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of this config item's value.
        /// </summary>
        public Type ValueType { get; }
    }

    /// <summary>
    /// Defines a name-only typed config item, which can be used to get or set the values of defining keys with the same name.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public interface ITypedConfigKey<T> : ITypedConfigKey
    { }
}