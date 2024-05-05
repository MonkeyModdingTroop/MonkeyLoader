using System;
using System.Collections.Generic;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents the typed definition for a ranged config item.
    /// </summary>
    /// <remarks>
    /// This class also implements <see cref="IConfigKeyValidator{T}"/> and therefore is two components in one.
    /// </remarks>
    /// <inheritdoc/>
    public sealed class ConfigKeyRange<T> : IConfigKeyRange<T>, IConfigKeyValidator<T>
    {
        /// <inheritdoc/>
        public IComparer<T?> Comparer { get; }

        /// <inheritdoc/>
        public T Max { get; }

        /// <inheritdoc/>
        public T Min { get; }

        /// <summary>
        /// Creates a new range component.
        /// </summary>
        /// <param name="min">The lower bound of the value range.</param>
        /// <param name="max">The upper bound of the value range.</param>
        /// <param name="comparer">The comparer to use to determine whether values fall into the range of this config item.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="min"/> or <paramref name="max"/> are null.</exception>
        /// <exception cref="NotSupportedException">When <paramref name="comparer"/> is null while <typeparamref name="T"/> is not <see cref="IComparable{T}"/></exception>
        public ConfigKeyRange(T? min = default, T? max = default, IComparer<T?>? comparer = null)
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

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<byte> WithMax(byte max) => new(byte.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<short> WithMax(short max) => new(short.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<int> WithMax(int max) => new(int.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<long> WithMax(long max) => new(long.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<float> WithMax(float max) => new(float.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<double> WithMax(double max) => new(double.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">Maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<decimal> WithMax(decimal max) => new(decimal.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<byte> WithMin(byte min) => new(min, byte.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<short> WithMin(short min) => new(min, short.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<int> WithMin(int min) => new(min, int.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<long> WithMin(long min) => new(min, long.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<float> WithMin(float min) => new(min, float.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<double> WithMin(double min) => new(min, double.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">Minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static ConfigKeyRange<decimal> WithMin(decimal min) => new(min, decimal.MaxValue);

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> config)
        {
            if (config.TryGetValue(out var value) && !IsValueInRange(value!))
                throw new InvalidOperationException($"Value for key [{config.Id}] did not pass validation!");
        }

        /// <summary>
        /// Determines whether the given value falls into this config item's range.
        /// </summary>
        /// <seealso cref="IsValueInRange(T)"/>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if the value is not null and falls into the range; otherwise, <c>false</c>.</returns>
        bool IConfigKeyValidator<T>.IsValid(T? value) => value is not null && IsValueInRange(value);

        /// <inheritdoc/>
        public bool IsValueInRange(T value)
            => Comparer.Compare(Min, value) <= 0 && Comparer.Compare(Max, value) >= 0;
    }

    /// <summary>
    /// Defines the typed definition for a ranged config item.
    /// </summary>
    /// <inheritdoc/>
    public interface IConfigKeyRange<T> : IConfigKeyComponent<IDefiningConfigKey<T>>
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
}