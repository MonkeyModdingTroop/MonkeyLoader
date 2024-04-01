﻿using MonkeyLoader.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using MonkeyLoader.Meta;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents the typed definition for a config item.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public class DefiningConfigKey<T> : IDefiningConfigKey<T>
    {
        private readonly Func<T>? _computeDefault;

        private readonly Lazy<string> _fullId;
        private readonly Predicate<T?>? _isValueValid;

        private ConfigSection? _configSection;
        private ConfigKeyChangedEventHandler? _untypedChanged;
        private T? _value;

        /// <inheritdoc/>
        public IConfigKey AsUntyped { get; }

        /// <inheritdoc/>
        public Config Config => Section.Config;

        /// <inheritdoc/>
        public string? Description { get; }

        /// <inheritdoc/>
        public string FullId => _fullId.Value;

        /// <inheritdoc/>
        public bool HasChanges { get; set; }

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(Description))]
        public bool HasDescription { get; }

        /// <inheritdoc/>
        public bool HasValue { get; private set; }

        /// <inheritdoc/>
        public string Id => AsUntyped.Id;

        /// <inheritdoc/>
        public bool InternalAccessOnly { get; }

        /// <inheritdoc/>
        public bool IsDefiningKey => true;

        /// <inheritdoc/>
        public ConfigSection Section
        {
            get => _configSection!;
            set
            {
                if (_configSection is not null)
                    throw new InvalidOperationException("ConfigSection can only be set once!");

                _configSection = value;
            }
        }

        /// <inheritdoc/>
        public Type ValueType { get; } = typeof(T);

        /// <summary>
        /// Gets the logger of the config this item belongs to if it's a <see cref="IsDefiningKey">defining key</see>.
        /// </summary>
        protected Logger Logger => Section.Config.Logger;

        /// <summary>
        /// Creates a new instance of the <see cref="DefiningConfigKey{T}"/> class with the given parameters.
        /// </summary>
        /// <param name="id">The mod-unique identifier of this config item. Must not be null or whitespace.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this config item. Otherwise everything will be accepted.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="id"/> is null or whitespace.</exception>
        public DefiningConfigKey(string id, string? description = null, Func<T>? computeDefault = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id), "Config key identifier must not be null or whitespace!");

            AsUntyped = new ConfigKey(id);

            Description = description;
            HasDescription = !string.IsNullOrWhiteSpace(description);

            _computeDefault = computeDefault;
            InternalAccessOnly = internalAccessOnly;
            _isValueValid = valueValidator;

            _fullId = new(() => $"{Config.Owner.Id}.{Section.Id}.{Id}");

            // Make the Compiler shut up about Section not being set - it gets set by the ConfigSection loading the keys.
            Section = default!;
        }

        /// <inheritdoc/>
        public bool Equals(IConfigKey other) => ConfigKey.EqualityComparer.Equals(this, other);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is IConfigKey otherKey && Equals(otherKey);

        /// <inheritdoc/>
        public override int GetHashCode() => ConfigKey.EqualityComparer.GetHashCode(this);

        /// <inheritdoc/>
        public T? GetValue()
        {
            TryGetValue(out T? value);
            return value;
        }

        object? IDefiningConfigKey.GetValue() => GetValue();

        /// <inheritdoc/>
        public void SetValue(T value, string? eventLabel = null)
        {
            if (!TrySetValue(value, eventLabel))
                throw new ArgumentException($"Tried to set key [{Id}] to invalid value!", nameof(value));
        }

        void IDefiningConfigKey.SetValue(object? value, string? eventLabel)
        {
            if (!((IDefiningConfigKey)this).TrySetValue(value, eventLabel))
                throw new ArgumentException($"Tried to set key [{Id}] to invalid value!", nameof(value));
        }

        /// <inheritdoc/>
        bool IDefiningConfigKey.TryComputeDefault(out object? defaultValue)
        {
            var success = TryComputeDefault(out T? defaultTypedValue);
            defaultValue = defaultTypedValue;

            return success;
        }

        /// <inheritdoc/>
        public bool TryComputeDefault(out T? defaultValue)
        {
            bool success;
            if (_computeDefault is null)
            {
                success = false;
                defaultValue = default;
            }
            else
            {
                success = true;
                defaultValue = _computeDefault();
            }

            if (!Validate((object?)defaultValue))
                throw new InvalidOperationException($"(Computed) default value for key [{Id}] did not pass validation!");

            return success;
        }

        /// <inheritdoc/>
        public bool TryGetValue(out T? value)
        {
            if (HasValue)
            {
                value = _value;
                return true;
            }

            if (TryComputeDefault(out value))
            {
                SetValue(value!, ConfigKey.SetFromDefaultEventLabel);
                return true;
            }

            value = default;
            return false;
        }

        bool IDefiningConfigKey.TryGetValue(out object? value)
        {
            var success = TryGetValue(out T? typedValue);
            value = typedValue;

            return success;
        }

        /// <inheritdoc/>
        public bool TrySetValue(T value, string? eventLabel = null)
        {
            if (!Validate(value))
                return false;

            var hadValue = HasValue;
            var oldValue = _value;

            _value = value;
            HasValue = true;

            OnChanged(hadValue, oldValue, eventLabel);

            return true;
        }

        bool IDefiningConfigKey.TrySetValue(object? value, string? eventLabel)
        {
            if (!Validate(value))
                return false;

            var hadValue = HasValue;
            var oldValue = _value;

            _value = (T)value!;
            HasValue = true;

            OnChanged(hadValue, oldValue, eventLabel);

            return true;
        }

        /// <inheritdoc/>
        public bool Unset()
        {
            var hadValue = HasValue;
            var oldValue = _value;

            _value = default;
            HasValue = false;

            OnChanged(hadValue, oldValue, nameof(IDefiningConfigKey.Unset));

            return hadValue;
        }

        /// <inheritdoc/>
        public bool Validate(T value) => _isValueValid?.Invoke(value) ?? true;

        /// <inheritdoc/>
        bool IDefiningConfigKey.Validate(object? value) => Validate(value);

        /// <summary>
        /// Handles the value of this config item potentially having changed.
        /// If the <paramref name="oldValue"/> and new value are different:<br/>
        /// Sets <see cref="HasChanges">HasChanges</see> and triggers this config item's typed and
        /// untyped <see cref="Changed">Changed</see> events and passes the event up to
        /// the <see cref="Configuration.Config"/> owning this item.
        /// </summary>
        /// <param name="hadValue">Whether the old value existed.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="eventLabel">The custom label that may be set by whoever changed the config.</param>
        protected virtual void OnChanged(bool hadValue, T? oldValue, string? eventLabel)
        {
            // Don't fire event if value didn't change
            if (ReferenceEquals(oldValue, _value) || (oldValue is not null && _value is not null && _value.Equals(oldValue)))
                return;

            HasChanges = true;
            var eventArgs = new ConfigKeyChangedEventArgs<T>(Config, this, hadValue, oldValue, HasValue, _value, eventLabel);

            try
            {
                Changed?.TryInvokeAll(this, eventArgs);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some typed {nameof(Changed)} event subscriber(s) of key [{Id}] threw an exception:"));
            }

            try
            {
                _untypedChanged?.TryInvokeAll(this, eventArgs);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some untyped {nameof(Changed)} event subscriber(s) of key [{Id}] threw an exception:"));
            }

            Config.OnItemChanged(eventArgs);
        }

        private bool Validate(object? value)
            => (value is T || (value is null && Util.CanBeNull(ValueType))) && Validate((T)value!);

        /// <inheritdoc/>
        public event ConfigKeyChangedEventHandler<T>? Changed;

        event ConfigKeyChangedEventHandler? IDefiningConfigKey.Changed
        {
            add => _untypedChanged += value;
            remove => _untypedChanged -= value;
        }
    }

    /// <summary>
    /// Defines the definition for a config item.
    /// </summary>
    public interface IDefiningConfigKey : ITypedConfigKey
    {
        /// <summary>
        /// Gets the config this item belongs to.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets the human-readable description of this config item.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the fully unique identifier for this config item.
        /// </summary>
        /// <remarks>
        /// Format:
        /// <c>$"{<see cref="Config">Config</see>.<see cref="Config.Owner">Owner</see>.<see cref="IConfigOwner.Id">Id</see>}.{<see cref="Section">Section</see>.<see cref="ConfigSection.Id">Id</see>}.{<see cref="IConfigKey.Id">Id</see>}"</c>
        /// </remarks>
        public string FullId { get; }

        /// <summary>
        /// Gets or sets whether this config item has unsaved changes.
        /// </summary>
        public bool HasChanges { get; set; }

        /// <summary>
        /// Gets whether this config item has a useable <see cref="Description">description</see>.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Description))]
        public bool HasDescription { get; }

        /// <summary>
        /// Gets whether this config item has a set value.
        /// </summary>
        /// <remarks>
        /// Should be automatomatically set to <c>true</c> when a different value is <see cref="SetValue">set</see>.
        /// </remarks>
        public bool HasValue { get; }

        /// <summary>
        /// Gets whether only the owning mod should have access to this config item.
        /// </summary>
        public bool InternalAccessOnly { get; }

        /// <summary>
        /// Gets the <see cref="ConfigSection"/> this item belongs to.
        /// </summary>
        /// <remarks>
        /// Should only be set once when the owning <see cref="ConfigSection"/> is initializing.
        /// </remarks>
        public ConfigSection Section { get; set; }

        /// <summary>
        /// Gets this config item's set value, falling back to the <see cref="TryComputeDefault">computed default</see>.
        /// </summary>
        /// <returns>The item's internal value or its <see cref="ValueType">type's</see> <c>default</c>.</returns>
        public object? GetValue();

        /// <summary>
        /// Set the config item's internal value to the given one.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="eventLabel">The custom label that may be set by whoever changed the config.</param>
        /// <exception cref="ArgumentException">The <paramref name="value"/> didn't pass <see cref="Validate">validation</see>.</exception>
        public void SetValue(object? value, string? eventLabel = null);

        /// <summary>
        /// Tries to compute the default value for this key, if a default provider was set.
        /// </summary>
        /// <param name="defaultValue">The computed default value if the return value is <c>true</c>. Otherwise <c>default</c>.</param>
        /// <returns><c>true</c> if the default value was successfully computed.</returns>
        public bool TryComputeDefault(out object? defaultValue);

        /// <summary>
        /// Tries to get this config item's set value, falling back to the <see cref="TryComputeDefault">computed default</see>.
        /// </summary>
        /// <param name="value">The item's internal value or its <see cref="ValueType">type's</see> <c>default</c>.</param>
        /// <returns><c>true</c> if the config item's value was set or a default could be computer, otherwise <c>false</c>.</returns>
        public bool TryGetValue(out object? value);

        /// <summary>
        /// Tries to set the config item's internal value to the given one if it passes <see cref="Validate">validation</see>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="eventLabel">The custom label that may be set by whoever changed the config.</param>
        /// <returns><c>true</c> if the value was successfully set, otherwise <c>false</c>.</returns>
        public bool TrySetValue(object? value, string? eventLabel = null);

        /// <summary>
        /// Removes this config item's value, setting it to its <see cref="ValueType">type's</see> <c>default</c>.
        /// </summary>
        /// <returns>Whether there was a value to remove.</returns>
        public bool Unset();

        /// <summary>
        /// Checks if a value is valid for this config item.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value is valid.</returns>
        public bool Validate(object? value);

        /// <summary>
        /// Triggered when the internal value of this config item changes.
        /// </summary>
        public event ConfigKeyChangedEventHandler? Changed;
    }

    /// <summary>
    /// Defines the typed definition for a config item.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public interface IDefiningConfigKey<T> : IDefiningConfigKey, ITypedConfigKey<T>
    {
        /// <summary>
        /// Gets this config item's set value, falling back to the <see cref="TryComputeDefault">computed default</see>.
        /// </summary>
        /// <returns>The item's internal value or its <see cref="ValueType">type's</see> <c>default</c>.</returns>
        public new T? GetValue();

        /// <summary>
        /// Set the config item's internal value to the given one.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="eventLabel">The custom label that may be set by whoever changed the config.</param>
        /// <exception cref="ArgumentException">Tthe <paramref name="value"/> didn't pass <see cref="Validate">validation</see>.</exception>
        public void SetValue(T value, string? eventLabel = null);

        /// <summary>
        /// Tries to compute the default value for this key, if a default provider was set.
        /// </summary>
        /// <param name="defaultValue">The computed default value if the return value is <c>true</c>. Otherwise <c>default</c>.</param>
        /// <returns><c>true</c> if the default value was successfully computed.</returns>
        public bool TryComputeDefault(out T? defaultValue);

        /// <summary>
        /// Tries to get this config item's set value, falling back to the <see cref="TryComputeDefault">computed default</see>.
        /// </summary>
        /// <param name="value">The item's internal value or its <see cref="ValueType">type's</see> <c>default</c>.</param>
        /// <returns><c>true</c> if the config item's value was set or a default could be computer, otherwise <c>false</c>.</returns>
        public bool TryGetValue(out T? value);

        /// <summary>
        /// Tries to set the config item's internal value to the given one if it passes <see cref="Validate">validation</see>.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="eventLabel">The custom label that may be set by whoever changed the config.</param>
        /// <returns><c>true</c> if the value was successfully set, otherwise <c>false</c>.</returns>
        public bool TrySetValue(T value, string? eventLabel = null);

        /// <summary>
        /// Checks if a value is valid for this config item.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value is valid.</returns>
        public bool Validate(T value);

        /// <summary>
        /// Triggered when the internal value of this config item changes.
        /// </summary>
        public new event ConfigKeyChangedEventHandler<T>? Changed;
    }
}