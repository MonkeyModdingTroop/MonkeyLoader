namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Defines the non-generic interface for the data tags of <see cref="ITaggable"/>s.
    /// </summary>
    public interface IDataTag : ITag
    {
        /// <summary>
        /// Gets the boxed data associated with this tag instance.
        /// </summary>
        public object? Data { get; }
    }

    /// <summary>
    /// Defines the generic interface for the data tags of <see cref="ITaggable"/>s.
    /// </summary>
    public interface IDataTag<T> : IDataTag
    {
        /// <summary>
        /// Gets the data associated with this tag instance.
        /// </summary>
        public new T Data { get; }
    }
}