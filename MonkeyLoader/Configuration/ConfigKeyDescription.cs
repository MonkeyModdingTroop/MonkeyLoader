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
        /// <param name="description"></param>
        public ConfigKeyDescription(string description)
        {
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