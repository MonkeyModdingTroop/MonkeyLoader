using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Meta.Tagging
{
    /// <summary>
    /// Defines the interface for object that support tagging using <see cref="ITag"/>s.
    /// </summary>
    public interface ITaggable
    {
        /// <summary>
        /// Gets the tags of this object.
        /// </summary>
        public TagCollection Tags { get; }
    }
}