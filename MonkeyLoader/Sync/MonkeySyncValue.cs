using EnumerableToolkit;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Defines the non-generic interface for <see cref="IMonkeySyncValue"/>s.
    /// </summary>
    public interface IMonkeySyncValue : INotifyValueChanged
    {
        /// <summary>
        /// Gets or sets the internal value of this sync value.
        /// </summary>
        public object? Value { get; set; }
    }

    /// <summary>
    /// Defines the generic interface for <see cref="IMonkeySyncValue"/>s.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Value">Value</see>.</typeparam>
    public interface IMonkeySyncValue<T> : INotifyValueChanged<T>,
        IReadOnlyMonkeySyncValue<T>, IWriteOnlyMonkeySyncValue<T>
    {
        /// <inheritdoc cref="IMonkeySyncValue.Value"/>
        public new T Value { get; set; }
    }

    /// <summary>
    /// Defines the interface for readonly <see cref="IMonkeySyncValue"/>s.
    /// </summary>
    /// <remarks>
    /// This interface exists purely to facilitate keeping a covariant list of sync values.
    /// </remarks>
    /// <typeparam name="T">The type of the <see cref="Value">Value</see>.</typeparam>
    public interface IReadOnlyMonkeySyncValue<out T> : IMonkeySyncValue
    {
        /// <summary>
        /// Gets the internal value of this sync value.
        /// </summary>
        public new T Value { get; }
    }

    /// <summary>
    /// Defines the interface for writeonly <see cref="IMonkeySyncValue"/>s.
    /// </summary>
    /// <remarks>
    /// This interface exists purely to facilitate keeping a contravariant list of sync values.
    /// </remarks>
    /// <typeparam name="T">The type of the <see cref="Value">Value</see>.</typeparam>
    public interface IWriteOnlyMonkeySyncValue<in T> : IMonkeySyncValue
    {
        /// <summary>
        /// Sets the internal value of this sync value.
        /// </summary>
        public new T Value { set; }
    }

    /// <summary>
    /// Implements a basic version of an <see cref="IMonkeySyncValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Value">Value</see>.</typeparam>
    public class MonkeySyncValue<T> : IMonkeySyncValue<T>
    {
        private ValueChangedEventHandler? _untypedChanged;
        private T _value;

        /// <inheritdoc/>
        public T Value
        {
            get => _value;

            [MemberNotNull(nameof(_value))]
            set
            {
                var oldValue = _value;
                _value = value!;

                OnChanged(oldValue);
            }
        }

        object? IMonkeySyncValue.Value
        {
            get => Value;
            set => Value = (T)value!;
        }

        /// <summary>
        /// Creates a new sync object instance that wraps the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public MonkeySyncValue(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Wraps the given <paramref name="value"/> into a sync object.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public static implicit operator MonkeySyncValue<T>(T value) => new(value);

        /// <summary>
        /// Unwraps the <see cref="Value">Value</see> from the given sync object.
        /// </summary>
        /// <param name="syncValue">The sync object to unwrap.</param>
        public static implicit operator T(MonkeySyncValue<T> syncValue) => syncValue.Value;

        /// <summary>
        /// Handles the value of this config item potentially having changed.
        /// If the <paramref name="oldValue"/> and new value are different:<br/>
        /// Triggers this sync object's typed and untyped <see cref="Changed">Changed</see> events.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="changedProperty">The name of the changed property on the value.</param>
        /// <param name="changedCollection">The collection change arguments for the value.</param>
        private void OnChanged(T? oldValue, string? changedProperty = null, NotifyCollectionChangedEventArgs? changedCollection = null)
        {
            var sameReferences = ReferenceEquals(oldValue, _value);

            if (!sameReferences)
            {
                // Remove NotifyChanged integration from old value
                if (oldValue is INotifyPropertyChanged oldPropertyChanged)
                    oldPropertyChanged.PropertyChanged -= ValuePropertyChanged;

                if (oldValue is INotifyCollectionChanged oldCollectionChanged)
                    oldCollectionChanged.CollectionChanged -= ValueCollectionChanged;

                // Add NotifyChanged integration to new value
                if (_value is INotifyPropertyChanged newPropertyChanged)
                    newPropertyChanged.PropertyChanged += ValuePropertyChanged;

                if (_value is INotifyCollectionChanged newCollectionChanged)
                    newCollectionChanged.CollectionChanged += ValueCollectionChanged;
            }

            // Don't fire event if it wasn't triggered by event and the value didn't change
            if ((sameReferences && changedProperty is null && changedCollection is null)
             || (oldValue is not null && _value is not null && _value.Equals(oldValue)))
                return;

            var eventArgs = new ValueChangedEventArgs<T>(oldValue, _value, changedProperty, changedCollection);

            var exceptions = new List<Exception>();

            try
            {
                Changed?.TryInvokeAll(this, eventArgs);
            }
            catch (AggregateException ex)
            {
                exceptions.Add(ex);
            }

            try
            {
                _untypedChanged?.TryInvokeAll(this, eventArgs);
            }
            catch (AggregateException ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
                throw new AggregateException($"Some {nameof(Changed)} event subscriber(s) of this MonkeySyncValue threw an exception.", exceptions);
        }

        private void ValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
            => OnChanged(_value, null, eventArgs);

        private void ValuePropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
            => OnChanged(_value, eventArgs.PropertyName);

        /// <inheritdoc/>
        public event ValueChangedEventHandler<T>? Changed;

        event ValueChangedEventHandler? INotifyValueChanged.Changed
        {
            add => _untypedChanged += value;
            remove => _untypedChanged -= value;
        }
    }
}