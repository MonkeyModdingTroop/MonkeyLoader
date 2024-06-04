using System.Collections.Generic;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for identifiable items that are the parents of <see cref="INestedIdentifiable"/>s.
    /// </summary>
    /// <remarks>
    /// Rather than implementing this, implement <see cref="IIdentifiableOwner{TOwner, TNestedIdentifiable}"/>.
    /// </remarks>
    /// <typeparam name="TNestedIdentifiable">The type of the nested <see cref="INestedIdentifiable"/> children.</typeparam>
    public interface IIdentifiableOwner<out TNestedIdentifiable> : IIdentifiable
        where TNestedIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Gets the nested child items.
        /// </summary>
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }

    /// <summary>
    /// Defines the interface for identifiable items that are the parents of concrete <see cref="INestedIdentifiable{TParent}"/>s.
    /// </summary>
    /// <typeparam name="TOwner">The implementing type and concrete parent's type for the <see cref="INestedIdentifiable{TParent}"/> children.</typeparam>
    /// <typeparam name="TNestedIdentifiable">The type of the nested <see cref="INestedIdentifiable{TParent}"/> children.</typeparam>
    public interface IIdentifiableOwner<in TOwner, out TNestedIdentifiable> : IIdentifiableOwner<TNestedIdentifiable>
        where TOwner : IIdentifiableOwner<TOwner, TNestedIdentifiable>
        where TNestedIdentifiable : INestedIdentifiable<TOwner>
    { }

    /// <summary>
    /// Defines the interface for identifiable items that are indirect parents of <see cref="INestedIdentifiable"/>s.
    /// </summary>
    /// <typeparam name="TNestedIdentifiable">The type of the indirectly nested <see cref="INestedIdentifiable"/> children.</typeparam>
    public interface INestedIdentifiableOwner<out TNestedIdentifiable> : IIdentifiable
        where TNestedIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Gets the indirect nested child items.
        /// </summary>
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }
}