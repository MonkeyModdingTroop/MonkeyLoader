using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Contains extension methods dealing with <see cref="IIdentifiable"/>s.
    /// </summary>
    public static class IdentifiableExtensions
    {
        /// <summary>
        /// Finds the nearest <typeparamref name="TParentIdentifiable"/> parent of this <paramref name="identifiable"/>
        /// that satisfies the given <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="TParentIdentifiable">The type of the parent <see cref="IIdentifiable"/> to look for.</typeparam>
        /// <param name="identifiable">The <see cref="IIdentifiable"/> to start looking from.</param>
        /// <param name="predicate">The predicate that a potential parent must satisfy.</param>
        /// <returns>The parent satisfying the <paramref name="predicate"/>.</returns>
        /// <exception cref="InvalidOperationException">When no suitable parent was found.</exception>
        public static TParentIdentifiable FindNearestParent<TParentIdentifiable>(this IIdentifiable identifiable, Predicate<TParentIdentifiable> predicate)
            where TParentIdentifiable : IIdentifiable
        {
            if (!identifiable.TryFindNearestParent(predicate, out var parentIdentifiable))
                throw new InvalidOperationException("No suitable parent found!");

            return parentIdentifiable;
        }

        /// <summary>
        /// Finds the nearest <typeparamref name="TParentIdentifiable"/> parent of this <paramref name="identifiable"/>.
        /// </summary>
        /// <typeparam name="TParentIdentifiable">The type of the parent <see cref="IIdentifiable"/> to look for.</typeparam>
        /// <param name="identifiable">The <see cref="IIdentifiable"/> to start looking from.</param>
        /// <returns>The found parent.</returns>
        /// <exception cref="InvalidOperationException">When no suitable parent was found.</exception>
        public static TParentIdentifiable FindNearestParent<TParentIdentifiable>(this IIdentifiable identifiable)
            where TParentIdentifiable : IIdentifiable
        {
            if (!identifiable.TryFindNearestParent(out TParentIdentifiable? parentIdentifiable))
                throw new InvalidOperationException("No suitable parent found!");

            return parentIdentifiable;
        }

        public static IIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        public static INestedIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);

        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        public static INestedIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.FullId);

        public static IEnumerable<TIdentifiable> GetAll<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => nestedIdentifiableCollection.Items;

        /// <summary>
        /// Tries to find the nearest <typeparamref name="TParentIdentifiable"/> parent of this <paramref name="identifiable"/>
        /// that satisfies the given <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="TParentIdentifiable">The type of the parent <see cref="IIdentifiable"/> to look for.</typeparam>
        /// <param name="identifiable">The <see cref="IIdentifiable"/> to start looking from.</param>
        /// <param name="predicate">The predicate that a potential parent must satisfy.</param>
        /// <param name="parentIdentifiable">The parent satisfying the <paramref name="predicate"/> if found; otherwise, <c>default</c>.</param>
        /// <returns><c>true</c> if a parent satisfying the <paramref name="predicate"/> was found; otherwise, <c>false</c>.</returns>
        public static bool TryFindNearestParent<TParentIdentifiable>(this IIdentifiable identifiable,
                Predicate<TParentIdentifiable> predicate, [NotNullWhen(true)] out TParentIdentifiable? parentIdentifiable)
            where TParentIdentifiable : IIdentifiable
        {
            while (identifiable.TryFindNearestParent(out parentIdentifiable))
            {
                if (predicate(parentIdentifiable))
                    return true;

                if (parentIdentifiable is not INestedIdentifiable)
                    break;

                identifiable = (INestedIdentifiable)parentIdentifiable;
            }

            parentIdentifiable = default;
            return false;
        }

        /// <summary>
        /// Tries to find the nearest <typeparamref name="TParentIdentifiable"/> parent of this <paramref name="identifiable"/>.
        /// </summary>
        /// <typeparam name="TParentIdentifiable">The type of the parent <see cref="IIdentifiable"/> to look for.</typeparam>
        /// <param name="identifiable">The <see cref="IIdentifiable"/> to start looking from.</param>
        /// <param name="parentIdentifiable">The parent if found; otherwise, <c>default</c>.</param>
        /// <returns><c>true</c> if a parent was found; otherwise, <c>false</c>.</returns>
        public static bool TryFindNearestParent<TParentIdentifiable>(this IIdentifiable identifiable, [NotNullWhen(true)] out TParentIdentifiable? parentIdentifiable)
            where TParentIdentifiable : IIdentifiable
        {
            while (identifiable is not TParentIdentifiable and INestedIdentifiable nestedCurrent)
                identifiable = nestedCurrent.Parent;

            if (identifiable is TParentIdentifiable parent)
            {
                parentIdentifiable = parent;
                return true;
            }

            parentIdentifiable = default;
            return false;
        }

        public static INestedIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.FullId);

        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        public static IIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        public static INestedIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);
    }
}