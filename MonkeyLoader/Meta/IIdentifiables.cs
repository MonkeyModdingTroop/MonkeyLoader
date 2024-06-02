namespace MonkeyLoader.Meta
{
    public interface IIdentifiable
    {
        /// <summary>
        /// Gets the unique identifier of this item.
        /// </summary>
        public string Id { get; }
    }

    public interface INestedIdentifiable : IIdentifiable
    {
        /// <summary>
        /// Gets the fully qualified identifier for this item.
        /// </summary>
        /// <value>
        /// <i>Should be:</i> <c>{<see cref="Parent">Parent</see>.(Full)Id}.{<see cref="IIdentifiable.Id">Id</see>}</c>
        /// </value>
        public string FullId { get; }

        /// <summary>
        /// Gets this item's <see cref="IIdentifiable">identifiable</see> parent.
        /// </summary>
        public IIdentifiable Parent { get; }
    }

    public interface INestedIdentifiable<out TParent> : INestedIdentifiable
        where TParent : IIdentifiable
    {
        /// <summary>
        /// Gets this item's <see cref="IIdentifiable">identifiable</see> parent.
        /// </summary>
        public new TParent Parent { get; }
    }
}