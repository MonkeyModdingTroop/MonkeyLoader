using MonkeyLoader.Meta.Tagging;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Defines the possible access levels for config sections or their items.
    /// </summary>
    public enum ConfigAccessLevel
    {
        /// <summary>
        /// The default level of access.<br/>
        /// This config section or item should always be shown to users.
        /// </summary>
        Regular,

        /// <summary>
        /// Access for advanced users.<br/>
        /// This config section or item should only be shown to advanced users.
        /// </summary>
        Advanced,

        /// <summary>
        /// Internal access of the mod only.<br/>
        /// This config section or item should never be shown to users.
        /// </summary>
        Internal
    }

    /// <summary>
    /// Defines the <see cref="ConfigAccessLevel"/> tag.
    /// </summary>
    /// <remarks>
    /// This tag should be used for config sections or their items to define when or how they can be accessed.
    /// </remarks>
    public sealed class ConfigAccessLevelTag : DataTag<ConfigAccessLevel>
    {
        /// <inheritdoc/>
        public override string Description => "Tags a config section or its items with an access level.";

        /// <inheritdoc/>
        public override string Id => nameof(ConfigAccessLevel);

        /// <summary>
        /// Creates a new instance of this tag with the given access <paramref name="level"/>.
        /// </summary>
        /// <param name="level">The access level to store.</param>
        public ConfigAccessLevelTag(ConfigAccessLevel level) : base(level)
        { }
    }
}