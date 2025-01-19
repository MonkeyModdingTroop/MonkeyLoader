using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for something with (potentially) dedicated author information.
    /// </summary>
    public interface IAuthorable
    {
        /// <summary>
        /// Gets the names of the authors of this authorable item.
        /// </summary>
        public IEnumerable<string> Authors { get; }

        /// <summary>
        /// Determines whether a given <paramref name="name"/>
        /// is listed as an author for this authorable item.
        /// </summary>
        /// <param name="name">The name to check for.</param>
        /// <returns><c>true</c> if the given <paramref name="name"/> is listed as an author for this authorable item.</returns>
        public bool HasAuthor(string name);
    }
}