using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for a search of an
    /// <see cref="IIdentifiableOwner{TNestedIdentifiable}"/>'s
    /// direct <see cref="INestedIdentifiable"/> child items.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="INestedIdentifiable"/> child items.</typeparam>
    public interface IIdentifiableOwnerSearch<out TIdentifiable> : INestedIdentifiableOwnerSearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Searches for a direct child item by its given <see cref="IIdentifiable.Id">id</see>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifiable.Id"/> of the item to search for.</param>
        /// <returns>The found item.</returns>
        /// <exception cref="KeyNotFoundException">When no item with the given id was found.</exception>
        public TIdentifiable ById(string id);
    }

    /// <summary>
    /// Defines the interface for a try-search of an
    /// <see cref="IIdentifiableOwner{TNestedIdentifiable}"/>'s
    /// direct <see cref="INestedIdentifiable"/> child items.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="INestedIdentifiable"/> child items.</typeparam>
    public interface IIdentifiableOwnerTrySearch<TIdentifiable> : INestedIdentifiableOwnerTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Tries searching for a direct child item by its given <see cref="IIdentifiable.Id">id</see>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifiable.Id"/> of the item to search for.</param>
        /// <param name="item">The item if found; otherwise, <c>default(<typeparamref name="TIdentifiable"/>)</c>.</param>
        /// <returns><c>true</c> if an item was found; otherwise, <c>false</c>.</returns>
        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable? item);
    }

    /// <summary>
    /// Defines the interface for a search of an
    /// <see cref="INestedIdentifiableOwner{TNestedIdentifiable}"/>'s
    /// indirect <see cref="INestedIdentifiable"/> child items.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the indirect <see cref="INestedIdentifiable"/> child items.</typeparam>
    public interface INestedIdentifiableOwnerSearch<out TIdentifiable> : INestedIdentifiableSearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Searches for an indirect child item by the given partial id,
        /// which is what remains of its <see cref="IIdentifiable.FullId">FullId</see> after the search root's FullId.
        /// </summary>
        /// <param name="partialId">The partial <see cref="IIdentifiable.FullId"/> of the item to search for.</param>
        /// <returns>The found item.</returns>
        /// <exception cref="KeyNotFoundException">When no item with the given partial id was found.</exception>
        public TIdentifiable ByPartialId(string partialId);
    }

    /// <summary>
    /// Defines the interface for a try-search of an
    /// <see cref="INestedIdentifiableOwner{TNestedIdentifiable}"/>'s
    /// indirect <see cref="INestedIdentifiable"/> child items.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the indirect <see cref="INestedIdentifiable"/> child items.</typeparam>
    public interface INestedIdentifiableOwnerTrySearch<TIdentifiable> : INestedIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Tries searching for an indirect child item by the given partial id,
        /// which is what remains of its <see cref="IIdentifiable.FullId">FullId</see> after the search root's FullId.
        /// </summary>
        /// <param name="partialId">The partial <see cref="IIdentifiable.FullId"/> of the item to search for.</param>
        /// <param name="item">The item if found; otherwise, <c>default(<typeparamref name="TIdentifiable"/>)</c>.</param>
        /// <returns><c>true</c> if an item was found; otherwise, <c>false</c>.</returns>
        public bool ByPartialId(string partialId, [NotNullWhen(true)] out TIdentifiable? item);
    }

    /// <summary>
    /// Defines the interface for a search of an <see cref="INestedIdentifiable"/> item.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="INestedIdentifiable"/> items.</typeparam>
    public interface INestedIdentifiableSearch<out TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Searches for an item by its given <see cref="IIdentifiable.FullId">id</see>.
        /// </summary>
        /// <param name="fullId">The <see cref="IIdentifiable.FullId"/> of the item to search for.</param>
        /// <returns>The found item.</returns>
        /// <exception cref="KeyNotFoundException">When no item with the given full id was found.</exception>
        public TIdentifiable ByFullId(string fullId);
    }

    /// <summary>
    /// Defines the interface for a try-search of an <see cref="INestedIdentifiable"/> item.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="INestedIdentifiable"/> items.</typeparam>
    public interface INestedIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Tries searching for an item by its given <see cref="IIdentifiable.FullId"/>.
        /// </summary>
        /// <param name="fullId">The <see cref="IIdentifiable.FullId"/> of the item to search for.</param>
        /// <param name="item">The item if found; otherwise, <c>default(<typeparamref name="TIdentifiable"/>)</c>.</param>
        /// <returns><c>true</c> if an item was found; otherwise, <c>false</c>.</returns>
        public bool ByFullId(string fullId, [NotNullWhen(true)] out TIdentifiable? item);
    }

    internal sealed class NestedIdentifiableSearch<TIdentifiable> : IIdentifiableOwnerSearch<TIdentifiable>, IIdentifiableOwnerTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        private readonly string? _baseId;
        private readonly IEnumerable<TIdentifiable> _items;

        internal NestedIdentifiableSearch(IEnumerable<TIdentifiable> items, string? baseId = null)
        {
            _items = items;
            _baseId = baseId;
        }

        /// <inheritdoc/>
        public TIdentifiable ByFullId(string fullId)
        {
            if (ByFullId(fullId, out var item))
                return item;

            throw new KeyNotFoundException($"No item with FullId [{fullId}] found!");
        }

        /// <inheritdoc/>
        public bool ByFullId(string fullId, [NotNullWhen(true)] out TIdentifiable? item)
        {
            if (_baseId is not null && !fullId.StartsWith(_baseId))
            {
                item = default;
                return false;
            }

            item = _items.FirstOrDefault(element => element.FullId.Equals(fullId, StringComparison.Ordinal));

            return item is not null;
        }

        /// <inheritdoc/>
        public TIdentifiable ById(string id)
        {
            if (ById(id, out var item))
                return item;

            throw new KeyNotFoundException($"No item with Id [{id}] found!");
        }

        /// <inheritdoc/>
        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable? item)
        {
            item = _items.FirstOrDefault(element => element.Id.Equals(id, StringComparison.Ordinal));

            return item is not null;
        }

        /// <inheritdoc/>
        public TIdentifiable ByPartialId(string partialId)
            => ByFullId(WithBaseId(partialId));

        /// <inheritdoc/>
        public bool ByPartialId(string partialId, [NotNullWhen(true)] out TIdentifiable? item)
            => ByFullId(WithBaseId(partialId), out item);

        private string WithBaseId(string partialId) => $"{_baseId}.{partialId}";
    }
}