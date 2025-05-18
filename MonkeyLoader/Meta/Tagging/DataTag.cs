namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Implements an abstract base class for any <see cref="IDataTag{T}"/>s.
    /// </summary>
    /// <typeparam name="T">The type of data associated with this tag type.</typeparam>
    public abstract class DataTag<T> : Tag, IDataTag<T>
    {
        /// <inheritdoc/>
        public T Data { get; }

        object? IDataTag.Data => Data;

        /// <summary>
        /// Creates a new instance of this tag type storing the given <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data associated with this instance.</param>
        protected DataTag(T data)
        {
            Data = data;
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"Data Tag: {Id} - {(Data is null ? "null" : Data.ToString())}";
    }
}