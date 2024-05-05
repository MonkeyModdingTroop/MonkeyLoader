using System;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Description of a <see cref="IDefiningConfigKey"/>.
    /// </summary>
    public sealed class ConfigKeyDescription : IConfigKeyDescription
    {
        /// <inheritdoc/>
        public string Description { get; }

        /// <summary>
        /// Creates a new description component.
        /// </summary>
        /// <param name="description">Description of the config key. Must not be just whitespace.</param>
        /// <exception cref="ArgumentException">When the <paramref name="description"/> is just whitespace.</exception>
        public ConfigKeyDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description must not be just whitespace.");

            Description = description;
        }

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey config)
        { }
    }

    /// <summary>
    /// Description of a <see cref="IDefiningConfigKey"/>.
    /// </summary>
    public interface IConfigKeyDescription : IConfigKeyComponent<IDefiningConfigKey>
    {
        /// <summary>
        /// The description to be shown for the config key.
        /// </summary>
        public string Description { get; }
    }
}