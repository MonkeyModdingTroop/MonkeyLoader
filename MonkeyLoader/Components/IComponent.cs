namespace MonkeyLoader.Components
{
    /// <summary>
    /// Defines the interface for the components of entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IComponent<in TEntity> where TEntity : IEntity<TEntity>
    {
        /// <summary>
        /// Initializes this component when it's added to an
        /// entity's <see cref="IEntity{TEntity}.Components">component list</see>.
        /// </summary>
        /// <param name="entity">The entity this component was added to.</param>
        public void Initialize(TEntity entity);
    }
}