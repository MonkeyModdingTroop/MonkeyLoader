﻿using EnumerableToolkit;
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
    /// Defines the non-generic interface for <see cref="ILinkedMonkeySyncValue{TLink, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink, T}"/>
    public interface ILinkedMonkeySyncValue<out TLink> : INotifyValueChanged, IDisposable
    {
        /// <summary>
        /// Gets the <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}.LinkObject">LinkObject</see>
        /// of the <see cref="ILinkedMonkeySyncValue{TLink}.SyncObject">SyncObject</see> that this value belongs to.
        /// </summary>
        public TLink LinkObject { get; }

        /// <summary>
        /// Gets the property name of this sync value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the sync object that this value belongs to.
        /// </summary>
        public ILinkedMonkeySyncObject<TLink> SyncObject { get; }

        /// <summary>
        /// Gets or sets the internal value of this sync value.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets the concrete <see cref="Type"/> of
        /// the wrapped <see cref="Value">Value</see>.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Tries to restore the link of this sync value.
        /// </summary>
        /// <returns><c>true</c> if the link was successfully restored; otherwise, <c>false</c>.</returns>
        public bool TryRestoreLink();
    }

    /// <summary>
    /// Defines the generic interface for linked <see cref="MonkeySyncValue{TLink, T}"/>s.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object that this sync value links to.</typeparam>
    /// <typeparam name="T">The type of the <see cref="ILinkedMonkeySyncValue{T}.Value">Value</see>.</typeparam>
    public interface ILinkedMonkeySyncValue<out TLink, T> : INotifyValueChanged<T>,
        IReadOnlyMonkeySyncValue<TLink, T>, IWriteOnlyMonkeySyncValue<TLink, T>
    {
        /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink}.Value"/>
        public new T Value { get; set; }
    }

    /// <summary>
    /// Defines the interface for readonly <see cref="ILinkedMonkeySyncValue{TLink}"/>s.
    /// </summary>
    /// <remarks>
    /// This interface exists purely to facilitate keeping a covariant list of sync values.
    /// </remarks>
    /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink, T}"/>
    public interface IReadOnlyMonkeySyncValue<out TLink, out T> : ILinkedMonkeySyncValue<TLink>
    {
        /// <summary>
        /// Gets the internal value of this sync value.
        /// </summary>
        public new T Value { get; }
    }

    /// <summary>
    /// Defines the interface for not yet linked <see cref="MonkeySyncValue{TLink, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink, T}"/>
    public interface IUnlinkedMonkeySyncValue<TLink> : ILinkedMonkeySyncValue<TLink>
    {
        /// <summary>
        /// Establishes this sync value's association and link through the given sync object.
        /// </summary>
        /// <param name="syncObject">The sync object that this value belongs to.</param>
        /// <param name="name">The name of this sync value.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the established link is valid; otherwise, <c>false</c>.</returns>
        public bool EstablishLinkFor(ILinkedMonkeySyncObject<TLink> syncObject, string name, bool fromRemote);
    }

    /// <summary>
    /// Defines the interface for writeonly <see cref="ILinkedMonkeySyncValue{TLink}"/>s.
    /// </summary>
    /// <remarks>
    /// This interface exists purely to facilitate keeping a contravariant list of sync values.
    /// </remarks>
    /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink, T}"/>
    public interface IWriteOnlyMonkeySyncValue<out TLink, in T> : ILinkedMonkeySyncValue<TLink>
    {
        /// <summary>
        /// Sets the internal value of this sync value.
        /// </summary>
        public new T Value { set; }
    }

    /// <summary>
    /// Implements an abstract base for <see cref="ILinkedMonkeySyncValue{TLink, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncValue{TLink, T}"/>
    public abstract class MonkeySyncValue<TLink, T> : IUnlinkedMonkeySyncValue<TLink>, ILinkedMonkeySyncValue<TLink, T>
    {
        private static readonly Type _valueType = typeof(T);

        private bool _disposedValue;
        private ValueChangedEventHandler? _untypedChanged;
        private T _value;

        /// <inheritdoc/>
        public TLink LinkObject => SyncObject.LinkObject;

        /// <inheritdoc/>
        public string Name { get; private set; } = null!;

        /// <inheritdoc/>
        public ILinkedMonkeySyncObject<TLink> SyncObject { get; private set; } = null!;

        /// <inheritdoc/>
        public virtual T Value
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

        object? ILinkedMonkeySyncValue<TLink>.Value
        {
            get => Value;
            set => Value = (T)value!;
        }

        /// <inheritdoc/>
        public Type ValueType => _valueType;

        /// <summary>
        /// Creates a new sync value instance that wraps the given <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public MonkeySyncValue(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Ensures any unmanaged resources are <see cref="Dispose(bool)">disposed</see>.
        /// </summary>
        ~MonkeySyncValue()
        {
            Dispose(false);
        }

        /// <summary>
        /// Unwraps the <see cref="Value">Value</see> from the given sync object.
        /// </summary>
        /// <param name="syncValue">The sync object to unwrap.</param>
        public static implicit operator T(MonkeySyncValue<TLink, T> syncValue) => syncValue.Value;

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in the 'OnDisposing()' or 'OnFinalizing()' methods
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <remarks>
        /// Sets this sync value's <see cref="SyncObject">SyncObject</see>
        /// and <see cref="Name">Name</see> to the ones provided.<br/>
        /// Then calls the <see cref="EstablishLinkInternal">internal link method</see>.
        /// </remarks>
        /// <inheritdoc/>
        public bool EstablishLinkFor(ILinkedMonkeySyncObject<TLink> syncObject, string propertyName, bool fromRemote)
        {
            SyncObject = syncObject;
            Name = propertyName;

            return EstablishLinkInternal(fromRemote);
        }

        /// <inheritdoc/>
        public override string ToString() => Value?.ToString() ?? "";

        /// <inheritdoc/>
        public abstract bool TryRestoreLink();

        /// <remarks>
        /// Handles the aspects of establishing a link that are
        /// particular to <typeparamref name="TLink"/>s as a link object.<br/>
        /// The <see cref="SyncObject">SyncObject</see> and <see cref="Name">Name</see>
        /// have already been assigned by the <see cref="EstablishLinkFor">EstablishLinkFor</see>
        /// method that calls this.
        /// </remarks>
        /// <inheritdoc cref="IUnlinkedMonkeySyncValue{TLink}.EstablishLinkFor"/>
        protected abstract bool EstablishLinkInternal(bool fromRemote);

        /// <summary>
        /// Cleans up any managed resources as part of <see cref="Dispose()">disposing</see>.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Sets the <see cref="SyncObject">SyncObject</see> to <c>null</c>
        /// and the internal <see cref="Value">value</see> to its <c>default</c>.
        /// </remarks>
        protected virtual void OnDisposing()
        {
            SyncObject = null!;
            _value = default!;
        }

        /// <summary>
        /// Cleans up any unmanaged resources as part of
        /// <see cref="Dispose()">disposing</see> or finalization.
        /// </summary>
        protected virtual void OnFinalizing()
        { }

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
                OnDisposing();

            OnFinalizing();

            _disposedValue = true;
        }

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