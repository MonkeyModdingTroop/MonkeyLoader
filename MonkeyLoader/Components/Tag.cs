using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    public static class Tags
    {
        private static readonly Dictionary<string, ITag> _tagsById = new();

        public static IEnumerable<ITag> All => _tagsById.Values.AsSafeEnumerable();

        /// <summary>
        /// Gets an <see cref="IEqualityComparer{T}"/> that compares tags based on their <see cref="ITag.Id">Id</see>.
        /// </summary>
        public static IEqualityComparer<ITag?> EqualityComparer { get; } = new TagEqualityComparer();

        public static IEnumerable<ITag> AllOfCategories(IEnumerable<Type> categories)
            => AllOfCategories(categories.ToArray());

        public static IEnumerable<ITag> AllOfCategories(params Type[] categories)
            => _tagsById.Values.Where(tag => categories.Any(category => category.IsAssignableFrom(tag.GetType())));

        public static IEnumerable<TTagCategory> OfCategory<TTagCategory>(this IEnumerable<ITag> tags)
            where TTagCategory : ITag
            => tags.SelectCastable<ITag, TTagCategory>();

        public static IEnumerable<TTagCategory> AllOfCategory<TTagCategory>()
            where TTagCategory : ITag
            => _tagsById.Values.OfCategory<TTagCategory>();

        public static IEnumerable<ITag> AllOfCategory(Type category)
            => _tagsById.Values.OfCategory(category);
        public static IEnumerable<ITag> OfCategory(this IEnumerable<ITag> tags, Type category)
            => tags.Where(tag => category.IsAssignableFrom(tag.GetType()));

        public static ITag GetById(string id)
        {
            if (TryGetById(id, out var tag))
                return tag;

            throw new KeyNotFoundException($"No tag found for id: [{id}]");
        }

        public static ITag GetCanonical(ITag tag)
        {
            if (HasCanonical(tag, out var foundTag))
                return foundTag;

            _tagsById.Add(tag.Id, tag);
            return tag;
        }

        public static bool HasCanonical(ITag tag, [NotNullWhen(true)] out ITag? canonicalTag)
            => TryGetById(tag.Id, out canonicalTag);

        public static bool HasCanonical(ITag tag)
            => _tagsById.ContainsKey(tag.Id);

        public static bool TryGetById(string id, [NotNullWhen(true)] out ITag? tag)
            => _tagsById.TryGetValue(id, out tag);

        private sealed class TagEqualityComparer : IEqualityComparer<ITag?>
        {
            public bool Equals(ITag? x, ITag? y)
                => ReferenceEquals(x, y) || string.Equals(x?.Id, y?.Id);

            public int GetHashCode(ITag? obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Defines the interface for tags for <see cref="IEntity{TEntity}"/>s.
    /// </summary>
    /// <remarks>
    /// The values of all of its properties should not change between instances.
    /// </remarks>
    public interface ITag
    {
        /// <summary>
        /// Gets the description for this type of tag.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the unique identifier of this type of tag.
        /// </summary>
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
    /// Implements a base for plain tags for <see cref="IEntity{TEntity}"/>s.
    /// </summary>
    public abstract class Tag : ITag, IEquatable<Tag>, IEquatable<ITag>
    {
        /// <inheritdoc/>
        public abstract string Description { get; }

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <remarks>
        /// <i>By default:</i> the <see cref="Id">Id</see>.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string Name => Id;

        /// <inheritdoc/>
        public bool Equals(ITag other)
            => Tags.EqualityComparer.Equals(this, other);

        bool IEquatable<Tag>.Equals(Tag other)
            => Tags.EqualityComparer.Equals(this, other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is ITag tag && Equals(tag);

        /// <inheritdoc/>
        public override int GetHashCode()
            => Tags.EqualityComparer.GetHashCode(this);

        /// <summary>
        /// Gets the <see cref="Id">Id</see> of this tag.
        /// </summary>
        /// <returns>The <see cref="Id">Id</see> of this tag.</returns>
        public override string ToString() => Id;
    }
}