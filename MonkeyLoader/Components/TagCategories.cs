using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonkeyLoader.Components
{
    public static class TagCategories
    {
        private static readonly Dictionary<string, TagCategory> _categoriesById = new();

        public static IEnumerable<TagCategory> All => _categoriesById.Values.AsSafeEnumerable();

        /// <summary>
        /// Gets an <see cref="IEqualityComparer{T}"/> that compares tags based on their <see cref="TagCategory.Id">Id</see>.
        /// </summary>
        public static IEqualityComparer<TagCategory?> EqualityComparer { get; } = new TagEqualityComparer();

        public static TagCategory GetById(string id)
        {
            if (TryGetById(id, out var tag))
                return tag;

            throw new KeyNotFoundException($"No tag category found for id: [{id}]");
        }

        public static TagCategory GetCanonical(TagCategory tagCategory)
        {
            if (HasCanonical(tagCategory, out var foundTag))
                return foundTag;

            _categoriesById.Add(tagCategory.Id, tagCategory);
            return tagCategory;
        }

        public static bool HasCanonical(TagCategory tagCategory, [NotNullWhen(true)] out TagCategory? canonicalTagCategory)
            => TryGetById(tagCategory.Id, out canonicalTagCategory);

        public static bool HasCanonical(TagCategory tagCategory)
            => _categoriesById.ContainsKey(tagCategory.Id);

        public static bool TryGetById(string id, [NotNullWhen(true)] out TagCategory? tag)
            => _categoriesById.TryGetValue(id, out tag);

        private sealed class TagEqualityComparer : IEqualityComparer<TagCategory?>
        {
            public bool Equals(TagCategory? x, TagCategory? y)
                => ReferenceEquals(x, y) || string.Equals(x?.Id, y?.Id);

            public int GetHashCode(TagCategory? obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }
}