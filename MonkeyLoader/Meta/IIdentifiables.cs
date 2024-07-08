namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for any identifiable item.
    /// </summary>
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the fully qualified identifier for this item.<br/>
        /// Should be implemented explicitly in non-<see cref="INestedIdentifiable"/>s.
        /// </summary>
        /// <value>
        /// <i>Should be:</i>
        /// <c>{<see cref="INestedIdentifiable.Parent">Parent</see>.<see cref="FullId">FullId</see>}.{<see cref="Id">Id</see>}</c>,
        /// or just <c><see cref="Id">Id</see></c> for top level items.
        /// </value>
        public string FullId { get; }

        /// <summary>
        /// Gets the category-unique identifier of this item.
        /// </summary>
        public string Id { get; }
    }

    /// <summary>
    /// Defines the interface for nested identifiable items,
    /// i.e. those that have an <see cref="IIdentifiable"/> parent.
    /// </summary>
    /// <remarks>
    /// Rather than implementing this, implement <see cref="INestedIdentifiable{TParent}"/>.
    /// </remarks>
    public interface INestedIdentifiable : IIdentifiable
    {
        /// <summary>
        /// Gets this item's <see cref="IIdentifiable">identifiable</see> parent.
        /// </summary>
        public IIdentifiable Parent { get; }
    }

    /// <summary>
    /// Defines the interface for nested identifiable items,
    /// i.e. those that have a concrete <see cref="IIdentifiable"/> <typeparamref name="TParent"/>.
    /// </summary>
    public interface INestedIdentifiable<out TParent> : INestedIdentifiable
        where TParent : IIdentifiable
    {
        /// <summary>
        /// Gets this item's <see cref="IIdentifiable">identifiable</see> parent.
        /// </summary>
        public new TParent Parent { get; }
    }
}