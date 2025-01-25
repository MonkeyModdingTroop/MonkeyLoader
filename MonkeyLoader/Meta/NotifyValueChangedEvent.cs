using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// The delegate that is called for an <see cref="INotifyValueChanged"/>'s
    /// <see cref="INotifyValueChanged.Changed">changed event</see>.
    /// </summary>
    /// <param name="sender">The object that sent the event.</param>
    /// <param name="valueChangedEventArgs">The event containing details about the change.</param>
    public delegate void ValueChangedEventHandler(object sender, IValueChangedEventArgs valueChangedEventArgs);

    /// <summary>
    /// The delegate that is called for an <see cref="INotifyValueChanged{T}"/>'s
    /// <see cref="INotifyValueChanged{T}.Changed">changed event</see>.
    /// </summary>
    /// <typeparam name="T">The type of the key's value.</typeparam>
    /// <param name="sender">The object that sent the event.</param>
    /// <param name="valueChangedEventArgs">The event containing details about the change.</param>
    public delegate void ValueChangedEventHandler<T>(object sender, ValueChangedEventArgs<T> valueChangedEventArgs);

    /// <summary>
    /// Defines the generic interface for objects that wrap another value
    /// and <see cref="Changed">notify</see> others about changes to it.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public interface INotifyValueChanged<T> : INotifyValueChanged
    {
        /// <summary>
        /// Triggered when the internal value wrapped by this object changes.
        /// </summary>
        public new event ConfigKeyChangedEventHandler<T>? Changed;
    }

    /// <summary>
    /// Defines the non-generic interface for objects that wrap another value
    /// and <see cref="Changed">notify</see> others about changes to it.
    /// </summary>
    public interface INotifyValueChanged
    {
        /// <summary>
        /// Triggered when the internal value wrapped by this object changes.
        /// </summary>
        public event ValueChangedEventHandler? Changed;
    }

    /// <summary>
    /// Represents a non-generic <see cref="ValueChangedEventArgs{T}"/>.
    /// </summary>
    public interface IValueChangedEventArgs
    {
        /// <summary>
        /// Gets the changed collection event arguments,
        /// if this configuration item's changed value originated
        /// from an <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
        /// </summary>
        public NotifyCollectionChangedEventArgs? ChangedCollection { get; }

        /// <summary>
        /// Gets the name of the property that changed,
        /// if this configuration item's changed value originated
        /// from an <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        public string? ChangedProperty { get; }

        /// <summary>
        /// Gets whether this configuration item's changed value originated
        /// from an <see cref="INotifyCollectionChanged.CollectionChanged"/> event.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="ChangedCollection">ChangedCollection</see> is not <c>null</c>; otherwise, <c>false</c>.
        /// </value>
        [MemberNotNullWhen(true, nameof(ChangedCollection))]
        public bool IsChangedCollection { get; }

        /// <summary>
        /// Gets whether this configuration item's changed value originated
        /// from an <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="ChangedProperty">ChangedProperty</see> is not <c>null</c>; otherwise, <c>false</c>.
        /// </value>
        [MemberNotNullWhen(true, nameof(ChangedProperty))]
        public bool IsChangedProperty { get; }

        /// <summary>
        /// Gets the new value of the configuration item.<br/>
        /// This can be the default value.
        /// </summary>
        public object? NewValue { get; }

        /// <summary>
        /// Gets the old value of the configuration item.<br/>
        /// This can be the default value.
        /// </summary>
        public object? OldValue { get; }
    }

    /// <summary>
    /// Represents the data for the <see cref="INotifyValueChanged.Changed"/>
    /// and <see cref="INotifyValueChanged{T}.Changed"/> events.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public class ValueChangedEventArgs<T> : IValueChangedEventArgs
    {
        /// <inheritdoc/>
        public NotifyCollectionChangedEventArgs? ChangedCollection { get; }

        /// <inheritdoc/>
        public string? ChangedProperty { get; }

        /// <inheritdoc/>
        public bool HadValue { get; }

        /// <inheritdoc/>
        public bool HasValue { get; }

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(ChangedCollection))]
        public bool IsChangedCollection => ChangedCollection is not null;

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(ChangedProperty))]
        public bool IsChangedProperty => ChangedProperty is not null;

        /// <summary>
        /// Gets the new value of the <see cref="DefiningConfigKey{T}"/>.<br/>
        /// This can be the default value.
        /// </summary>
        public T? NewValue { get; }

        object? IValueChangedEventArgs.NewValue => NewValue;

        /// <summary>
        /// Gets the old value of the <see cref="DefiningConfigKey{T}"/>.<br/>
        /// This can be the default value.
        /// </summary>
        public T? OldValue { get; }

        object? IValueChangedEventArgs.OldValue => OldValue;

        /// <summary>
        /// Creates a new event args instance for a changed config item.
        /// </summary>
        /// <param name="oldValue">The optional old value.</param>
        /// <param name="newValue">The optional new value.</param>
        /// <param name="changedProperty">The name of the changed property on the value.</param>
        /// <param name="changedCollection">The collection change arguments for the value.</param>
        public ValueChangedEventArgs(T? oldValue, T? newValue,
            string? changedProperty, NotifyCollectionChangedEventArgs? changedCollection)
        {
            OldValue = oldValue;
            NewValue = newValue;

            ChangedProperty = changedProperty;
            ChangedCollection = changedCollection;
        }
    }
}