using System.Diagnostics.CodeAnalysis;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for anything that should be displayed with a
    /// <see cref="Name">name</see> and <see cref="Description">description</see>.
    /// </summary>
    public interface IDisplayable
    {
        /// <summary>
        /// Gets the optional human-readable description to display for this item.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets whether this item has a <see cref="Description">description</see>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if <see cref="Description">Description</see> <see langword="is"/>
        /// <see langword="not"/> <see langword="null"/>; otherwise, <see langword="false"/>.</value>
        [MemberNotNullWhen(true, nameof(Description))]
        public bool HasDescription { get; }

        /// <summary>
        /// Gets the human-readable name to display for this item.
        /// </summary>
        public string Name { get; }
    }
}