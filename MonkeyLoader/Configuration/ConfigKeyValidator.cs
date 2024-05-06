using System;
using System.Text.RegularExpressions;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains some <see cref="IConfigKeyValidator{T}"/> presets.
    /// </summary>
    public static class ConfigKeyValidator
    {
        /// <summary>
        /// Gets a validator component that only accepts non-null non-whitespace strings.
        /// </summary>
        public static ConfigKeyValidator<string> NotNullOrWhitespace { get; } = new(value => !string.IsNullOrWhiteSpace(value));

        /// <summary>
        /// Creates a new validator component that only accepts strings,
        /// where the given <paramref name="regex"/> has a match.
        /// </summary>
        /// <param name="regex">The regular expression that must have a match.</param>
        /// <returns>The validator component.</returns>
        public static ConfigKeyValidator<string> Matching(Regex regex) => new(regex.IsMatch);
    }

    /// <summary>
    /// A validator component for a <see cref="IDefiningConfigKey{T}"/>.<br/>
    /// Multiple <see cref="IConfigKeyValidator{T}"/> instances on one config key must all validate successfully.
    /// </summary>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public sealed class ConfigKeyValidator<T> : IConfigKeyValidator<T>
    {
        private readonly Predicate<T?> _validator;

        /// <summary>
        /// Gets a validator component that only accepts non-null values.
        /// </summary>
        public static ConfigKeyValidator<T> NotNull { get; } = new(value => value is not null);

        /// <summary>
        /// Creates a new validator component using a predicate for validation.
        /// </summary>
        /// <param name="validator">The value validator.</param>
        public ConfigKeyValidator(Predicate<T?> validator)
        {
            _validator = validator;
        }

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> configKey)
        {
            if (configKey.HasValue && configKey.TryGetValue(out var value) && !IsValid(value))
                throw new InvalidOperationException($"Value for key [{configKey.Id}] did not pass validation!");
        }

        /// <inheritdoc/>
        public bool IsValid(T? value) => _validator(value);
    }

    /// <summary>
    /// A validator component for a <see cref="IDefiningConfigKey{T}"/>.<br/>
    /// Multiple components of this type on one config key must all validate successfully.
    /// </summary>
    /// <remarks>
    /// The validator must ensure that the config key's value is valid (if present) when the component is added.
    /// It is safe to throw an exception otherwise when initializing.
    /// </remarks>
    /// <typeparam name="T">The type of the config item's value.</typeparam>
    public interface IConfigKeyValidator<T> : IConfigKeyComponent<IDefiningConfigKey<T>>
    {
        /// <summary>
        /// Whether <paramref name="value"/> is valid for this <see cref="IDefiningConfigKey{T}"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid(T? value);
    }
}