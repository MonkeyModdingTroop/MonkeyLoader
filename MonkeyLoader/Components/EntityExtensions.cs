using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Contains helper methods to work with entities and their components.<br/>
    /// These are mostly extension methods that proxy calls to <see cref="IEntity{TEntity}.Components"/>
    /// </summary>
    public static class EntityExtensions
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
}