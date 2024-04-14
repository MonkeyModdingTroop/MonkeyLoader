using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Defines the definition for a ranged config item.
    /// </summary>
    /// <inheritdoc/>
    public interface IRangedDefiningKey : IDefiningConfigKey
    {
        /// <summary>
        /// Gets the upper bound of this config item's value range.
        /// </summary>
        public object Max { get; }

        /// <summary>
        /// Gets the lower bound of this config item's value range.
        /// </summary>
        public object Min { get; }

        /// <summary>
        /// Determines whether the given value falls into this config item's range.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if the value falls into the range; otherwise, <c>false</c>.</returns>
        public bool IsValueInRange(object value);
    }

    /// <summary>
    /// Defines the typed definition for a ranged config item.
    /// </summary>
    /// <inheritdoc/>
    public interface IRangedDefiningKey<T> : IDefiningConfigKey<T>, IRangedDefiningKey
    {
        /// <summary>
        /// Gets the typed comparer used to check whether new values fall into this config item's range.
        /// </summary>
        public IComparer<T> Comparer { get; }

        /// <summary>
        /// Gets the typed upper bound of this config item's value range.
        /// </summary>
        public new T Max { get; }

        /// <summary>
        /// Gets the typed lower bound of this config item's value range.
        /// </summary>
        public new T Min { get; }

        /// <summary>
        /// Determines whether the given value falls into this config item's range.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if the value falls into the range; otherwise, <c>false</c>.</returns>
        public bool IsValueInRange(T value);
    }

    /// <summary>
    /// Represents the typed definition for a ranged config item.
    /// </summary>
    /// <inheritdoc/>
    public class RangedDefiningConfigKey<T> : DefiningConfigKey<T>, IRangedDefiningKey<T>
    {
        /// <inheritdoc/>
        public IComparer<T?> Comparer { get; }

        /// <inheritdoc/>
        public T Max { get; }

        object IRangedDefiningKey.Max => Max!;

        /// <inheritdoc/>
        public T Min { get; }

        object IRangedDefiningKey.Min => Min!;

        /// <summary>
        /// Creates a new instance of the <see cref="DefiningConfigKey{T}"/> class with the given parameters.
        /// </summary>
        /// <param name="id">The mod-unique identifier of this config item. Must not be null or whitespace.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="min">The lower bound of the value range.</param>
        /// <param name="max">The upper bound of the value range.</param>
        /// <param name="comparer">The comparer to use to determine whether values fall into the range of this config item.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this config item. Otherwise everything will be accepted.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="id"/> is null or whitespace; or when <paramref name="min"/> or <paramref name="max"/> are null.</exception>
        /// <exception cref="NotSupportedException">When <paramref name="comparer"/> is null while <typeparamref name="T"/> is not <see cref="IComparable{T}"/></exception>
        public RangedDefiningConfigKey(string id, string? description = null, Func<T>? computeDefault = null,
            T? min = default, T? max = default, IComparer<T?>? comparer = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
            : base(id, description, computeDefault, internalAccessOnly, valueValidator)
        {
            if (min is null)
                throw new ArgumentNullException(nameof(min));

            if (max is null)
                throw new ArgumentNullException(nameof(max));

            if (comparer is null && !typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
                throw new NotSupportedException($"The {nameof(comparer)} must not be null when {nameof(T)} is not IComparable<T>!");

            Min = min;
            Max = max;
            Comparer = comparer ?? Comparer<T>.Default!;
        }

        /// <inheritdoc/>
        public bool IsValueInRange(T value)
            => Comparer.Compare(Min, value) <= 0 && Comparer.Compare(Max, value) >= 0;

        bool IRangedDefiningKey.IsValueInRange(object value)
            => value is T typedValue && IsValueInRange(typedValue);

        /// <inheritdoc/>
        public override bool Validate(T value)
            => base.Validate(value) && IsValueInRange(value);
    }
}