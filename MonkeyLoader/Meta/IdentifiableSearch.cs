using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    public interface IIdentifiableSearch<out TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
        public TIdentifiable ById(string id);
    }

    public interface IIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
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

        public TIdentifiable ById(string id)
        {
            if (ById(id, out var item))
                return item;

            throw new KeyNotFoundException($"No item with Id [{id}] found!");
        }

        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable item)
        {
            item = _items.FirstOrDefault(element => element.Id.Equals(id, StringComparison.Ordinal));

            return item is not null;
        }
    }
}