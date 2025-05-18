namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Defines the interface for the presence tags of <see cref="ITaggable"/>s.
    /// </summary>
    public interface ITag
    {
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
}