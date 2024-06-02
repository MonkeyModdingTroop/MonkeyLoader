using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    public static class IdentifiableExtensions
    {
        public static IIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        public static INestedIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);

        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.GetTrueId());

        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.GetTrueId());

        public static INestedIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.GetTrueId());

        public static IEnumerable<TIdentifiable> GetAll<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => nestedIdentifiableCollection.Items;

        public static string GetTrueId(this IIdentifiable identifiable)
            => identifiable is INestedIdentifiable nestedIdentifiable ? nestedIdentifiable.FullId : identifiable.Id;

        public static INestedIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.GetTrueId());

        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.GetTrueId());

        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.GetTrueId());

        public static IIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        public static INestedIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);
    }
}