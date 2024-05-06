namespace MonkeyLoader.Components
{
    /// <summary>
    /// Defines the interface for entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IEntity<out TEntity> where TEntity : IEntity<TEntity>
    {
        /// <summary>
        /// Gets the entity's component list.
        /// </summary>
        public IComponentList<TEntity> Components { get; }
    }
}