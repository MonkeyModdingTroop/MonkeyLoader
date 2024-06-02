using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    public interface IIdentifiableOwnerSearch<out TIdentifiable> : INestedIdentifiableOwnerSearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public TIdentifiable ById(string id);
    }

    public interface IIdentifiableOwnerTrySearch<TIdentifiable> : INestedIdentifiableOwnerTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable? item);
    }

    public interface INestedIdentifiableOwnerSearch<out TIdentifiable> : INestedIdentifiableSearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public TIdentifiable ByPartialId(string partialId);
    }

    public interface INestedIdentifiableOwnerTrySearch<TIdentifiable> : INestedIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public bool ByPartialId(string partialId, [NotNullWhen(true)] out TIdentifiable? item);
    }

    public interface INestedIdentifiableSearch<out TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public TIdentifiable ByFullId(string fullId);
    }

    public interface INestedIdentifiableTrySearch<TIdentifiable>
        where TIdentifiable : INestedIdentifiable
    {
        public bool ByFullId(string id, [NotNullWhen(true)] out TIdentifiable? item);
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

        public TIdentifiable ByFullId(string fullId)
        {
            if (ByFullId(fullId, out var item))
                return item;

            throw new KeyNotFoundException($"No item with FullId [{fullId}] found!");
        }

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

        public TIdentifiable ById(string id)
        {
            if (ById(id, out var item))
                return item;

            throw new KeyNotFoundException($"No item with Id [{id}] found!");
        }

        public bool ById(string id, [NotNullWhen(true)] out TIdentifiable? item)
        {
            item = _items.FirstOrDefault(element => element.Id.Equals(id, StringComparison.Ordinal));

            return item is not null;
        }

        public TIdentifiable ByPartialId(string partialId)
            => ByFullId(WithBaseId(partialId));

        public bool ByPartialId(string partialId, [NotNullWhen(true)] out TIdentifiable? item)
            => ByFullId(WithBaseId(partialId), out item);

        private string WithBaseId(string partialId) => $"{_baseId}.{partialId}";
    }
}