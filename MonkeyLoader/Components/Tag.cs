using System;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Components
{
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