using MonkeyLoader.Logging;
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
    public abstract class DefiningConfigKeyWrapper<TValue> : IDefiningConfigKeyWrapper<TValue>
    {
        private ConfigKeyChangedEventHandler? _untypedChanged;

        /// <inheritdoc/>
        IConfigKey IConfigKey.AsUntyped => Key.AsUntyped;

        /// <inheritdoc/>
        public Config Config => Key.Config;

        /// <inheritdoc/>
        public string? Description => Key.Description;

        /// <inheritdoc/>
        public string FullId => Key.FullId;

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
        public string Id => Key.Id;

        /// <inheritdoc/>
        public bool InternalAccessOnly => Key.InternalAccessOnly;

        /// <inheritdoc/>
        public bool IsDefiningKey => Key.IsDefiningKey;

        /// <summary>
        /// Gets the wrapped defining config key.
        /// </summary>
        public IDefiningConfigKey<TValue> Key { get; }

        IDefiningConfigKey IConfigKeyWrapper<IDefiningConfigKey>.Key => Key;

        IConfigKey IConfigKeyWrapper.Key => Key;

        /// <inheritdoc/>
        public ConfigSection Section
        {
            get => Key.Section;
            set => Key.Section = value;
        }

        /// <inheritdoc/>
        public Type ValueType => Key.ValueType;

        /// <summary>
        /// Gets the logger of the config this item belongs to if it's a <see cref="IsDefiningKey">defining key</see>.
        /// </summary>
        protected Logger Logger => Key.Section.Config.Logger;

        /// <summary>
        /// Wraps the given defining config key.
        /// </summary>
        /// <param name="definingKey">The defining key to wrap.</param>
        protected DefiningConfigKeyWrapper(IDefiningConfigKey<TValue> definingKey)
        {
            Key = definingKey;
            definingKey.Changed += OnTypedChange;
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

        private void OnTypedChange(object sender, ConfigKeyChangedEventArgs<TValue> configKeyChangedEventArgs)
        {
            var eventArgs = new ConfigKeyChangedEventArgs<TValue>(Config, this,
                configKeyChangedEventArgs.HadValue, configKeyChangedEventArgs.OldValue,
                configKeyChangedEventArgs.HasValue, configKeyChangedEventArgs.NewValue,
                configKeyChangedEventArgs.Label,
                configKeyChangedEventArgs.ChangedProperty,
                configKeyChangedEventArgs.ChangedCollection);

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

        /// <inheritdoc/>
        public event ConfigKeyChangedEventHandler<TValue>? Changed;

        event ConfigKeyChangedEventHandler? IDefiningConfigKey.Changed
        {
            add => _untypedChanged += value;
            remove => _untypedChanged -= value;
        }
    }

    /// <summary>
    /// Defines the interface for <see cref="IDefiningConfigKey"/> wrappers.
    /// </summary>
    public interface IDefiningConfigKeyWrapper : IDefiningConfigKey, IConfigKeyWrapper<IDefiningConfigKey>
    { }

    /// <summary>
    /// Defines the interface for <see cref="IDefiningConfigKey{T}"/> wrappers.
    /// </summary>
    /// <typeparam name="T">The typeparameter of the wrapped <see cref="IDefiningConfigKey{T}"/>.</typeparam>
    public interface IDefiningConfigKeyWrapper<T> : IDefiningConfigKey<T>, IDefiningConfigKeyWrapper, IConfigKeyWrapper<IDefiningConfigKey<T>>
    { }
}