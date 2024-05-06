using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Represents a basic base for <see cref="IEntity{TEntity}"/> instances,
    /// that <see cref="IComponent{TEntity}"/> instances can belong to.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public abstract class Entity<TEntity> : IEntity<TEntity>
        where TEntity : Entity<TEntity>
    {
        /// <inheritdoc/>
        public IComponentList<TEntity> Components { get; }

        /// <summary>
        /// Creates a new entity instance, using <c>this</c> as the
        /// <see cref="IComponentList{TEntity}.Entity">Entity</see> for the
        /// <see cref="Entity{TEntity}.Components">component list</see>.
        /// </summary>
        protected Entity()
        {
            Components = new ComponentList<TEntity>((TEntity)this);
        }

        IEnumerator<IComponent<TEntity>> IEnumerable<IComponent<TEntity>>.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();
    }
}