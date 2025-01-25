using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
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
    public interface IMonkeySyncValue<T> : IReadOnlyMonkeySyncValue<T>, IWriteOnlyMonkeySyncValue<T>
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

    public class MonkeySyncValue<T> : IMonkeySyncValue<T>
    {
        private T _value;

        /// <inheritdoc/>
        public T Value
        {
            get => _value;
            set
            {
                if (ReferenceEquals(_value, value))
                    return;

                _value = value;
            }
        }

        object? IMonkeySyncValue.Value
        {
            get => Value;
            set => Value = (T)value!;
        }
    }
}