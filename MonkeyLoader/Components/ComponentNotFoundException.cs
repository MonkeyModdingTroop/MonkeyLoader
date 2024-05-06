using System;

namespace MonkeyLoader.Components
{
    /// <summary>
    /// Represents the error when no matching component could be found in an
    /// <see cref="IComponentList{TEntity}.Get{TComponent}()"/> or
    /// <see cref="IComponentList{TEntity}.Get{TComponent}(Predicate{TComponent})"/> method.
    /// </summary>
    public sealed class ComponentNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new instance of this exception.
        /// </summary>
        public ComponentNotFoundException() : base("No component of matching type found!")
        { }
    }
}