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

        /// <param name="identifiableCollection">The identifiable collection to start from.</param>
        /// <inheritdoc cref="Get{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static IIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        /// <param name="nestedIdentifiableCollection">The nested identifiable collection to start from.</param>
        /// <inheritdoc cref="Get{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static INestedIdentifiableSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);

        /// <summary>
        /// Starts a search for a child <typeparamref name="TIdentifiable"/> from this object.
        /// </summary>
        /// <typeparam name="TOwner">The type of the identifiable owner to start from.</typeparam>
        /// <typeparam name="TIdentifiable">The type of the nested <see cref="IIdentifiable"/> to find.</typeparam>
        /// <param name="identifiableOwner">The identifiable owner to start from.</param>
        /// <returns>The search object for it.</returns>
        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        /// <inheritdoc cref="Get{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static IIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        /// <param name="nestedIdentifiableOwner">The nested identifiable owner to start from.</param>
        /// <inheritdoc cref="Get{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static INestedIdentifiableOwnerSearch<TIdentifiable> Get<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.FullId);

        /// <param name="nestedIdentifiableCollection">The nested identifiable collection to start from.</param>
        /// <inheritdoc cref="GetAll{TIdentifiable}(IIdentifiableCollection{TIdentifiable})"/>
        public static IEnumerable<TIdentifiable> GetAll<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => nestedIdentifiableCollection.Items;

        /// <summary>
        /// Gets an enumerable of all child <typeparamref name="TIdentifiable"/> from this object.
        /// </summary>
        /// <typeparam name="TIdentifiable">The type of the nested <see cref="IIdentifiable"/>s to enumerate.</typeparam>
        /// <param name="identifiableCollection">The identifiable collection to start from.</param>
        /// <returns>An enumerable of all child <typeparamref name="TIdentifiable"/>.</returns>
        public static IEnumerable<TIdentifiable> GetAll<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => identifiableCollection.Items;

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

        /// <param name="nestedIdentifiableOwner">The nested identifiable owner to start from.</param>
        /// <inheritdoc cref="TryGet{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static INestedIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableOwner<TIdentifiable> nestedIdentifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableOwner.Items, nestedIdentifiableOwner.FullId);

        /// <summary>
        /// Starts a try-search for a child <typeparamref name="TIdentifiable"/> from this object.
        /// </summary>
        /// <returns>The try-search object for it.</returns>
        /// <inheritdoc cref="Get{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TOwner, TIdentifiable>(this IIdentifiableOwner<TOwner, TIdentifiable> identifiableOwner)
            where TOwner : IIdentifiableOwner<TOwner, TIdentifiable>
            where TIdentifiable : INestedIdentifiable<TOwner>
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        /// <inheritdoc cref="TryGet{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static IIdentifiableOwnerTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableOwner<TIdentifiable> identifiableOwner)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(identifiableOwner.Items, identifiableOwner.FullId);

        /// <param name="identifiableCollection">The identifiable collection to start from.</param>
        /// <inheritdoc cref="TryGet{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static IIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this IIdentifiableCollection<TIdentifiable> identifiableCollection)
            where TIdentifiable : IIdentifiable
            => new IdentifiableSearch<TIdentifiable>(identifiableCollection.Items);

        /// <param name="nestedIdentifiableCollection">The nested identifiable collection to start from.</param>
        /// <inheritdoc cref="TryGet{TOwner, TIdentifiable}(IIdentifiableOwner{TOwner, TIdentifiable})"/>
        public static INestedIdentifiableTrySearch<TIdentifiable> TryGet<TIdentifiable>(this INestedIdentifiableCollection<TIdentifiable> nestedIdentifiableCollection)
            where TIdentifiable : INestedIdentifiable
            => new NestedIdentifiableSearch<TIdentifiable>(nestedIdentifiableCollection.Items);
    }
}