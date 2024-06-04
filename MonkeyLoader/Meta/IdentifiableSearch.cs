using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for a search of a root <see cref="IIdentifiable"/> item.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="IIdentifiable"/> items.</typeparam>
    public interface IIdentifiableSearch<out TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
        /// <summary>
        /// Searches for an item by its given <see cref="IIdentifiable.Id">id</see>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifiable.Id"/> of the item to search for.</param>
        /// <returns>The found item.</returns>
        /// <exception cref="KeyNotFoundException">When no item with the given id was found.</exception>
        public TIdentifiable ById(string id);
    }

    /// <summary>
    /// Defines the interface for a try-search of a root <see cref="IIdentifiable"/> item.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="IIdentifiable"/> items.</typeparam>
    public interface IIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
        /// <summary>
        /// Tries searching for an item by its given <see cref="IIdentifiable.Id">id</see>.
        /// </summary>
        /// <param name="id">The <see cref="IIdentifiable.Id"/> of the item to search for.</param>
        /// <param name="item">The item if found; otherwise, <c>default(<typeparamref name="TIdentifiable"/>)</c>.</param>
        /// <returns><c>true</c> if an item was found; otherwise, <c>false</c>.</returns>
        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable item);
    }

    internal sealed class IdentifiableSearch<TIdentifiable> : IIdentifiableSearch<TIdentifiable>, IIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
        private readonly IEnumerable<TIdentifiable> _items;

        internal IdentifiableSearch(IEnumerable<TIdentifiable> items)
        {
            _items = items;
        }

        /// <inheritdoc/>
        public TIdentifiable ById(string id)
        {
            if (ById(id, out var item))
                return item;

            throw new KeyNotFoundException($"No item with Id [{id}] found!");
        }

        /// <inheritdoc/>
        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable item)
        {
            item = _items.FirstOrDefault(element => element.Id.Equals(id, StringComparison.Ordinal));

            return item is not null;
        }
    }
}