using System;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Defines the interface for the components of entities.
    /// </summary>
    /// <remarks>
    /// More specific component interfaces can be created as such:
    /// <code>
    /// public interface IConfigKeyComponent&lt;in TKey&gt; : IComponent&lt;TKey&gt;
    ///     where TKey : class, IDefiningConfigKey, IEntity&lt;TKey&gt;
    /// { }
    /// </code>
    /// </remarks>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IComponent<in TEntity> where TEntity : class, IEntity<TEntity>
    {
        /// <summary>
        /// Initializes this component when it's added to an
        /// entity's <see cref="IEntity{TEntity}.Components">component list</see>.<br/>
        /// This may throw a <see cref="InvalidOperationException"/> when the state of the given entity is invalid for this component.
        /// </summary>
        /// <param name="entity">The entity this component was added to.</param>
        /// <exception cref="InvalidOperationException">When the state of the given entity is invalid.</exception>
        public void Initialize(TEntity entity);
    }
}