namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Implements an abstract base class for any <see cref="ITag"/>s.
    /// </summary>
    public abstract class Tag : ITag
    {
        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <remarks>
        /// <i>By default:</i> The <see cref="Id">Id</see> of this tag.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string Name => Id;

        /// <inheritdoc/>
        public override string ToString()
            => $"Presence Tag: {Id}";
    }
}