namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Defines the non-generic interface for the tags of <see cref="ITaggable"/>s.
    /// </summary>
    public interface ITag
    {
        /// <summary>
        /// Gets the data associated with this tag instance.
        /// </summary>
        public object? Data { get; }

        /// <summary>
        /// Gets the description for this type of tag.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the unique identifier of this type of tag.
        /// </summary>
        /// <remarks>
        /// Case should be ignored when using this.
        /// </remarks>
        public string Id { get; }

        /// <summary>
        /// Gets the name for this type of tag.
        /// </summary>
        /// <remarks>
        /// Implementations may default to the <see cref="Id">Id</see>.
        /// </remarks>
        public string Name { get; }
    }

    /// <summary>
    /// Defines the generic interface for the tags of <see cref="ITaggable"/>s.
    /// </summary>
    public interface ITag<T> : ITag
    {
        /// <summary>
        /// Gets the data associated with this tag instance.
        /// </summary>
        public new T Data { get; }
    }
}