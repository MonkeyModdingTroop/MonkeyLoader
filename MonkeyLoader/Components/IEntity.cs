using System.Collections.Generic;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Defines the interface for entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IEntity<TEntity> : IEnumerable<IComponent<TEntity>>
        where TEntity : class, IEntity<TEntity>
    {
        /// <summary>
        /// Gets the entity's component list.
        /// </summary>
        public IComponentList<TEntity> Components { get; }
    }
}