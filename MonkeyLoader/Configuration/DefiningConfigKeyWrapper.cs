using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Base class for defining config key wrappers.
    /// </summary>
    public abstract class DefiningConfigKeyWrapper<TValue> : IDefiningConfigKey<TValue>
    {
        /// <inheritdoc/>
        public Config Config => Key.Config;

        /// <inheritdoc/>
        public string? Description => Key.Description;

        /// <inheritdoc/>
        public bool HasChanges
        {
            get => Key.HasChanges;
            set => Key.HasChanges = value;
        }

        /// <inheritdoc/>
        public bool HasDescription => Key.HasDescription;

        /// <inheritdoc/>
        public bool HasValue => Key.HasValue;

        /// <inheritdoc/>
        public bool InternalAccessOnly => Key.InternalAccessOnly;

        /// <inheritdoc/>
        public bool IsDefiningKey => Key.IsDefiningKey;

        /// <summary>
        /// Gets the wrapped defining config key.
        /// </summary>
        public IDefiningConfigKey<TValue> Key { get; }

        /// <inheritdoc/>
        public string Name => Key.Name;

        /// <inheritdoc/>
        public ConfigSection Section
        {
            get => Key.Section;
            set => Key.Section = value;
        }

        /// <inheritdoc/>
        public Type ValueType => Key.ValueType;

        /// <summary>
        /// Wraps the given defining config key.
        /// </summary>
        /// <param name="definingKey">The defining key to wrap.</param>
        protected DefiningConfigKeyWrapper(IDefiningConfigKey<TValue> definingKey)
        {
            Key = definingKey;
        }

        /// <inheritdoc/>
        public bool Equals(IConfigKey other) => Key.Equals(other);

        /// <inheritdoc/>
        public TValue? GetValue() => Key.GetValue();

        object? IDefiningConfigKey.GetValue() => ((IDefiningConfigKey)Key).GetValue();

        /// <inheritdoc/>
        public void SetValue(TValue value, string? eventLabel = null) => Key.SetValue(value, eventLabel);

        /// <inheritdoc/>
        public void SetValue(object? value, string? eventLabel) => Key.SetValue(value, eventLabel);

        /// <inheritdoc/>
        public bool TryComputeDefault(out TValue? defaultValue) => Key.TryComputeDefault(out defaultValue);

        /// <inheritdoc/>
        public bool TryComputeDefault(out object? defaultValue) => Key.TryComputeDefault(out defaultValue);

        /// <inheritdoc/>
        public bool TryGetValue(out TValue? value) => Key.TryGetValue(out value);

        /// <inheritdoc/>
        public bool TryGetValue(out object? value) => Key.TryGetValue(out value);

        /// <inheritdoc/>
        public bool TrySetValue(TValue value, string? eventLabel = null) => Key.TrySetValue(value, eventLabel);

        /// <inheritdoc/>
        public bool TrySetValue(object? value, string? eventLabel) => Key.TrySetValue(value, eventLabel);

        /// <inheritdoc/>
        public bool Unset() => Key.Unset();

        /// <inheritdoc/>
        public bool Validate(TValue value) => Key.Validate(value);

        /// <inheritdoc/>
        public bool Validate(object? value) => Key.Validate(value);

        /// <inheritdoc/>
        public event ConfigKeyChangedEventHandler<TValue>? Changed
        {
            add => Key.Changed += value;
            remove => Key.Changed -= value;
        }

        event ConfigKeyChangedEventHandler? IDefiningConfigKey.Changed
        {
            add => ((IDefiningConfigKey)Key).Changed += value;
            remove => ((IDefiningConfigKey)Key).Changed -= value;
        }
    }
}