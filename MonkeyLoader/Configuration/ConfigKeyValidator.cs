using System;
using System.Text.RegularExpressions;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// A validator component for a <see cref="IDefiningConfigKey{T}"/>. Multiple components of <see cref="IConfigKeyValidator{T}"/>
    /// on one config key must all validate correctly.
    /// </summary>
    /// <typeparam name="T">Inner value type of the config key.</typeparam>
    public sealed class ConfigKeyValidator<T> : IConfigKeyValidator<T>
    {
        private readonly Predicate<T?> _validator;

        /// <summary>
        /// Creates a new validator component that only accepts non-null values.
        /// </summary>
        /// <returns>The validator component.</returns>
        public static ConfigKeyValidator<T> NotNull => new(value => value is not null);

        /// <summary>
        /// Creates a new validator component that only accepts non-null non-whitespace strings.
        /// </summary>
        /// <returns>The validator component.</returns>
        public static ConfigKeyValidator<string> NotNullOrWhitespace { get; } = new(value => !string.IsNullOrWhiteSpace(value));

        /// <summary>
        /// Creates a new validator component using a predicate for validation.
        /// </summary>
        /// <param name="validator">The value validator.</param>
        public ConfigKeyValidator(Predicate<T?> validator)
        {
            _validator = validator;
        }

        /// <summary>
        /// Creates a new validator component that only accepts strings matching <paramref name="regex"/>.
        /// </summary>
        /// <param name="regex">The regular expression that must be matched.</param>
        /// <returns>The validator component.</returns>
        public static ConfigKeyValidator<string> Matching(Regex regex) => new(regex.IsMatch);

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> config)
        {
            if (config.TryGetValue(out var value) && !IsValid(value))
                throw new InvalidOperationException($"Value for key [{config.Id}] did not pass validation!");
        }

        /// <inheritdoc/>
        public bool IsValid(T? value) => _validator(value);
    }

    /// <summary>
    /// A validator component for a <see cref="IDefiningConfigKey{T}"/>. Multiple components of this type
    /// on one config key must all validate correctly.
    /// </summary>
    /// <remarks>
    /// The validator must ensure that the config key's value is valid (if present) when the component is added.
    /// It is safe to throw an exception otherwise when initializing.
    /// </remarks>
    /// <typeparam name="T">Inner value type of the config key.</typeparam>
    public interface IConfigKeyValidator<T> : IConfigKeyComponent<IDefiningConfigKey<T>>
    {
        /// <summary>
        /// Whether <paramref name="value"/> is valid for this <see cref="IDefiningConfigKey{T}"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid and <c>false</c> otherwise.</returns>
        public bool IsValid(T? value);
    }
}