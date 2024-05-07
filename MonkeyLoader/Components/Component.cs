using System;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Contains helper methods to work with entities and their components.<br/>
    /// These are mostly extension methods that proxy calls to <see cref="IEntity{TEntity}.Components"/>
    /// </summary>
    public static class Component
    {
        /// <summary>
        /// Adds the given component to this entity.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entity">The entity to add the component to.</param>
        /// <param name="component">The component to add to the entity.</param>
        public static void Add<TEntity>(this TEntity entity, IComponent<TEntity> component)
            where TEntity : class, IEntity<TEntity>
            => entity.Components.Add(component);
    }

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