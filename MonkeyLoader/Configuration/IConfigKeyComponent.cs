using MonkeyLoader.Components;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// A component of a <see cref="IDefiningConfigKey"/>, e.g. description or range.
    /// A component cannot be removed from a config key once it was added.
    /// </summary>
    public interface IConfigKeyComponent<in TKey> : IComponent<TKey>
        where TKey : IDefiningConfigKey, IEntity<TKey>
    { }
}