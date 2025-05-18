using System.Text;

namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Implements an abstract base class for any <see cref="ITag{T}"/>s.
    /// </summary>
    /// <typeparam name="T">The type of data associated with this tag type.</typeparam>
    public abstract class Tag<T> : ITag<T>
    {
        /// <inheritdoc/>
        public T Data { get; }

        object? ITag.Data => Data;

        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <remarks>
        /// <i>By default:</i> The <see cref="Id">Id</see> of this tag.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string Name => Id;

        /// <summary>
        /// Creates a new instance of this tag type storing the given <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data associated with this instance.</param>
        protected Tag(T data)
        {
            Data = data;
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{Id}-Tag: {(Data is null ? "null" : Data.ToString())}";
    }
}