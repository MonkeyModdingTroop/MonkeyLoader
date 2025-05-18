using System;
using System.Collections.Generic;

namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Represents a generic tag that can have any <see cref="Id">Id</see> and <see cref="DataTag{T}.Data">Data</see>.
    /// </summary>
    /// <inheritdoc/>
    public sealed class GenericTag<T> : DataTag<T>
    {
        private static readonly Dictionary<string, string> _descriptionsById = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public override string Description { get; }

        /// <inheritdoc/>
        public override string Id { get; }

        /// <summary>
        /// Creates a new generic tag instance with the given <paramref name="id"/> and <paramref name="data"/>.
        /// </summary>
        /// <param name="id">The unique identifier of this type of tag.</param>
        /// <param name="data">The data associated with this instance.</param>
        public GenericTag(string id, T data) : base(data)
        {
            Id = id;

            if (!_descriptionsById.TryGetValue(id, out var description))
            {
                description = $"GenericTag<{typeof(T).CompactDescription()}> with id: {id}";
                _descriptionsById.Add(id.ToLower(), description);
            }

            Description = description;
        }
    }
}