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

        TEntity IEntity<TEntity>.Self => (TEntity)this;

        /// <summary>
        /// Creates a new entity instance, using <c>this</c> as the
        /// <see cref="IComponentList{TEntity}.Entity">Entity</see> for the
        /// <see cref="Entity{TEntity}.Components">component list</see>.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the concrete type isn't assignable to <typeparamref name="TEntity"/>.</exception>
        protected Entity()
        {
            if (!typeof(TEntity).IsAssignableFrom(GetType()))
                throw new InvalidOperationException("Concrete type must be assignable to TEntity!");

            Components = new ComponentList<TEntity>((TEntity)this);
        }

        IEnumerator<IComponent<TEntity>> IEnumerable<IComponent<TEntity>>.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Components.GetAll<IComponent<TEntity>>().GetEnumerator();
    }

    /// <summary>
    /// Defines the interface for entities that <see cref="IComponent{TEntity}"/> instances belong to.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IEntity<TEntity> : IEnumerable<IComponent<TEntity>>
        where TEntity : class, IEntity<TEntity>
    {
        /// <summary>
        /// Gets this entity's component list.
        /// </summary>
        public IComponentList<TEntity> Components { get; }

        /// <summary>
        /// Gets the entity itself.
        /// </summary>
        /// <remarks>
        /// This allows methods that take in an entity to access
        /// the main <typeparamref name="TEntity"/> value from the entity,
        /// without having to deal with potential ambiguities by taking it in directly.
        /// </remarks>
        public TEntity Self { get; }
    }
}