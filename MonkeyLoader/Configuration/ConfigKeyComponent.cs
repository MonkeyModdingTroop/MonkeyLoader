using MonkeyLoader.Components;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains helper methods to work with config key entities and their components.<br/>
    /// These are mostly extension methods that proxy calls to <see cref="IEntity{TEntity}.Components"/>
    /// </summary>
    public static class ConfigKeyComponent
    {
        /// <summary>
        /// Adds the given config key component to this config key entity.
        /// </summary>
        /// <typeparam name="TKey">The type of the config key.</typeparam>
        /// <param name="configKey">The config key entity to add the component to.</param>
        /// <param name="component">The config key component to add to the entity.</param>
        public static void Add<TKey>(this IEntity<TKey> configKey, IConfigKeyComponent<TKey> component)
            where TKey : class, IDefiningConfigKey, IEntity<TKey>
            => configKey.Components.Add(component);
    }

    /// <summary>
    /// A component of a <see cref="IDefiningConfigKey"/>, e.g. description or range.
    /// A component cannot be removed from a config key once it was added.
    /// </summary>
    public interface IConfigKeyComponent<in TKey> : IComponent<TKey>
        where TKey : class, IDefiningConfigKey, IEntity<TKey>
    { }
}