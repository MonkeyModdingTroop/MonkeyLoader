using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
    public abstract class TagCategory : IEquatable<TagCategory>
    {
        public abstract string Description { get; }

        public abstract string Id { get; }

        public virtual string Name => Id;

        /// <inheritdoc/>
        public bool Equals(TagCategory other)
            => TagCategories.EqualityComparer.Equals(this, other);

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is ITag tag && Equals(tag);

        /// <inheritdoc/>
        public override int GetHashCode()
            => TagCategories.EqualityComparer.GetHashCode(this);

        /// <summary>
        /// Gets the <see cref="Id">Id</see> of this tag.
        /// </summary>
        /// <returns>The <see cref="Id">Id</see> of this tag.</returns>
        public override string ToString() => Id;

        private sealed class TagCategoryEqualityComparer : IEqualityComparer<TagCategory?>
        {
            public bool Equals(TagCategory? x, TagCategory? y)
                => ReferenceEquals(x, y) || string.Equals(x?.Id, y?.Id);

            public int GetHashCode(TagCategory? obj)
                => obj?.Id.GetHashCode() ?? 0;
        }
    }
}