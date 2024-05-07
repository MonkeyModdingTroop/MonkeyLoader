using System;
using System.Collections.Generic;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains some <see cref="IConfigKeyRange{T}"/> presets.
    /// </summary>
    public static class ConfigKeyRange
    {
        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<byte> WithMax(byte max) => new ConfigKeyRange<byte>(byte.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<short> WithMax(short max) => new ConfigKeyRange<short>(short.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<int> WithMax(int max) => new ConfigKeyRange<int>(int.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<long> WithMax(long max) => new ConfigKeyRange<long>(long.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<float> WithMax(float max) => new ConfigKeyRange<float>(float.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<double> WithMax(double max) => new ConfigKeyRange<double>(double.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a maximum value.
        /// </summary>
        /// <param name="max">The maximum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<decimal> WithMax(decimal max) => new ConfigKeyRange<decimal>(decimal.MinValue, max);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<byte> WithMin(byte min) => new ConfigKeyRange<byte>(min, byte.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<short> WithMin(short min) => new ConfigKeyRange<short>(min, short.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<int> WithMin(int min) => new ConfigKeyRange<int>(min, int.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<long> WithMin(long min) => new ConfigKeyRange<long>(min, long.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<float> WithMin(float min) => new ConfigKeyRange<float>(min, float.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<double> WithMin(double min) => new ConfigKeyRange<double>(min, double.MaxValue);

        /// <summary>
        /// Creates a new half-open range component with a minimum value.
        /// </summary>
        /// <param name="min">The minimum value (inclusive).</param>
        /// <returns>A new half-open range component.</returns>
        public static IConfigKeyRange<decimal> WithMin(decimal min) => new ConfigKeyRange<decimal>(min, decimal.MaxValue);
    }

    /// <summary>
    /// Represents the typed definition for a ranged config item.
    /// </summary>
    /// <remarks>
    /// This class also implements <see cref="IConfigKeyValidator{T}"/> and is therefore two components in one.
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
        public T Max { get; }

        /// <summary>
        /// Gets the typed lower bound of this config item's value range.
        /// </summary>
        public T Min { get; }

        /// <summary>
        /// Determines whether the given value falls into this config item's range.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> if the value falls into the range; otherwise, <c>false</c>.</returns>
        public bool IsValueInRange(T value);
    }
}