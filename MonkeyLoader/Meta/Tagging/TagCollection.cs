using EnumerableToolkit;
using NuGet.Packaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Stores an <see cref="ITaggable"/>'s <see cref="ITag"/>s.
    /// </summary>
    public sealed class TagCollection : IEnumerable<KeyValuePair<string, IEnumerable<ITag>>>
    {
        private readonly Dictionary<string, HashSet<ITag>> _tagsById = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the total number of <see cref="ITag"/>s stored in this collection.
        /// </summary>
        public int Count => _tagsById.Values.Sum(static tagSet => tagSet.Count);

        /// <summary>
        /// Gets the number of distinct <see cref="ITag.Id">Ids</see> of <see cref="ITag"/>s stored in this collection.
        /// </summary>
        public int IdCount => _tagsById.Count;

        /// <summary>
        /// Gets the distinct <see cref="ITag.Id">Ids</see> of <see cref="ITag"/>s stored in this collection.
        /// </summary>
        public IEnumerable<string> Ids => _tagsById.Keys;

        /// <summary>
        /// Gets all <see cref="ITag"/>s stored in this collection.
        /// </summary>
        public IEnumerable<ITag> Instances => _tagsById.Values.SelectMany(static set => set);

        /// <summary>
        /// Gets or sets the tags associated with the given <paramref name="id"/>.
        /// </summary>
        /// <remarks>
        /// This will return an empty enumerable instead of throwing
        /// when there is no <see cref="ITag"/> with the given <paramref name="id"/>.
        /// </remarks>
        /// <param name="id">The id of the tags to get or replace. Case is ignored.</param>
        /// <returns>All tags with the given <paramref name="id"/>.</returns>
        public IEnumerable<ITag> this[string id]
        {
            get => _tagsById.TryGetValue(id, out var tagSet) ? tagSet.AsSafeEnumerable() : [];

            set
            {
                if (!value.Any())
                {
                    _tagsById.Remove(id);
                    return;
                }

                var tagSet = GetOrCreateTagSetById(id);
                tagSet.Clear();
                tagSet.AddRange(value);
            }
        }

        /// <summary>
        /// Ensures that the given <paramref name="tag"/> is included in this collection.
        /// </summary>
        /// <param name="tag">The tag to include in this collection.</param>
        /// <returns><c>true</c> if the <paramref name="tag"/> was added; <c>false</c> if it was already present.</returns>
        public bool Add(ITag tag)
            => GetOrCreateTagSetById(tag.Id).Add(tag);

        /// <summary>
        /// Ensures that the given <paramref name="tags"/> are included in this collection.
        /// </summary>
        /// <param name="tags">The tags to include in this collection.</param>
        public void AddRange(IEnumerable<ITag> tags)
        {
            foreach (var tag in tags)
                Add(tag);
        }

        /// <summary>
        /// Tests whether any <see cref="ITag"/>s with the given <paramref name="id"/> are stored in this collection.
        /// </summary>
        /// <param name="id">The id of the tags to check for. Case is ignored.</param>
        /// <returns><c>true</c> if this collections stores any <see cref="ITag"/>s with the given <paramref name="id"/>; otherwise, <c>false</c>.</returns>
        public bool AnyWithId(string id)
            => _tagsById.ContainsKey(id);

        /// <summary>
        /// Removes all <see cref="ITag"/>s from this collection.
        /// </summary>
        public void Clear()
            => _tagsById.Clear();

        /// <summary>
        /// Tests whether the given <paramref name="tag"/> is stored in this collection.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <returns><c>true</c> if the given <paramref name="tag"/> is stored in this collection; otherwise, <c>false</c>.</returns>
        public bool Contains(ITag tag)
        {
            if (!_tagsById.TryGetValue(tag.Id, out var tagSet))
                return false;

            return tagSet.Contains(tag);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, IEnumerable<ITag>>> GetEnumerator()
            => _tagsById.Select(static tagPair => new KeyValuePair<string, IEnumerable<ITag>>(tagPair.Key, tagPair.Value.AsSafeEnumerable())).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Ensures that the given <paramref name="tag"/> is not stored in this collection.
        /// </summary>
        /// <param name="tag">The tag to exclude from this collection.</param>
        /// <returns><c>true</c> if the tag was removed; <c>false</c> if it wasn't stored in this collection.</returns>
        public bool Remove(ITag tag)
        {
            if (!_tagsById.TryGetValue(tag.Id, out var tagSet))
                return false;

            var removed = tagSet.Remove(tag);

            if (tagSet.Count is 0)
                _tagsById.Remove(tag.Id);

            return removed;
        }

        private HashSet<ITag> GetOrCreateTagSetById(string id)
        {
            if (!_tagsById.TryGetValue(id, out var tagSet))
            {
                tagSet = [];
                _tagsById.Add(id, tagSet);
            }

            return tagSet;
        }
    }
}