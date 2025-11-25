using EnumerableToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace MonkeyLoader.Utility
{
    /// <summary>
    /// Provides the ability to inject actions into the execution of an enumeration while transforming it.<br/><br/>
    /// This example shows how to apply the <see cref="AsyncEnumerableInjector{TIn, TOut}"/> when patching a function.<br/>
    /// Of course you typically wouldn't patch with a generic method, that's just for illustrating the Type usage.
    /// <code>
    /// private static void Postfix&lt;Original, Transformed&gt;(ref IEnumerable&lt;Original&gt; __result) where Transformed : Original
    /// {
    ///     __result = new AsyncEnumerableInjector&lt;Original, Transformed&gt;(__result,
    ///         item =&gt; { Msg("Change what the item is exactly"); return new Transformed(item); })
    ///     {
    ///         Prefix = () =&gt; Msg("Before the first item is returned"),
    ///         PreItem = item =&gt; { Msg("Decide if an item gets returned"); return true; },
    ///         PostItem = (original, transformed, returned) =&gt; Msg("After control would come back to the generator after a yield return"),
    ///         Postfix = () =&gt; Msg("When the generator stopped returning items")
    ///     };
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="TOriginal">The type of the original enumeration's items.</typeparam>
    /// <typeparam name="TTransformed">The type of the transformed enumeration's items.<br/>Must be assignable to <c>TOriginal</c> for compatibility.</typeparam>
    public class AsyncEnumerableInjector<TOriginal, TTransformed> : IAsyncEnumerable<TTransformed> where TTransformed : TOriginal
    {
        /// <summary>
        /// Internal enumerator for iteration.
        /// </summary>
        private readonly IAsyncEnumerator<TOriginal> _enumerator;

        private Func<Task> _postfix = async () => { };
        private Func<TOriginal, TTransformed, bool, Task> _postItem = async (original, transformed, returned) => { };
        private Func<TOriginal, TTransformed[], bool, Task> _postItems;
        private Func<Task> _prefix = async () => { };
        private Func<TOriginal, Task<bool>> _preItem = async item => true;
        private Func<TOriginal, Task<TTransformed>> _transformItem = item => throw new NotImplementedException("You're supposed to insert your own transformation function here when not using TransformItems!");
        private Func<TOriginal, IAsyncEnumerable<TTransformed>> _transformItems;

        /// <summary>
        /// Gets called when the wrapped enumeration returned the last item.
        /// </summary>
        public Func<Task> Postfix
        {
            get => _postfix;

            [MemberNotNull(nameof(_postfix))]
            set => _postfix = value ?? throw new ArgumentNullException(nameof(value), "Postfix can't be null!");
        }

        /// <summary>
        /// Gets called for each item, with the transformed item, and whether it was passed through.
        /// First thing to be called after execution returns to the enumerator after a yield return.
        /// </summary>
        /// <remarks>
        /// Called by the default <see cref="TransformItems">TransformItems</see> for each item, but not directly.
        /// </remarks>
        public Func<TOriginal, TTransformed, bool, Task> PostItem
        {
            get => _postItem;

            [MemberNotNull(nameof(_postItem))]
            set => _postItem = value ?? throw new ArgumentNullException(nameof(value), "PostItem can't be null!");
        }

        /// <summary>
        /// Gets called for each item, with the transformed items, and whether they were passed through.
        /// First thing to be called after execution returns to the enumerator after a yield return.
        /// </summary>
        /// <remarks>
        /// Has precedence over <see cref="PostItem">PostItem</see> - the default implementation just passes the call through for each item.
        /// </remarks>
        public Func<TOriginal, TTransformed[], bool, Task> PostItems
        {
            get => _postItems;

            [MemberNotNull(nameof(_postItems))]
            set => _postItems = value ?? throw new ArgumentNullException(nameof(value), "PostItems can't be null!");
        }

        /// <summary>
        /// Gets called before the enumeration returns the first item.
        /// </summary>
        public Func<Task> Prefix
        {
            get => _prefix;

            [MemberNotNull(nameof(_prefix))]
            set => _prefix = value ?? throw new ArgumentNullException(nameof(value), "Prefix can't be null!");
        }

        /// <summary>
        /// Gets called for each item to determine whether it should be passed through.
        /// </summary>
        public Func<TOriginal, Task<bool>> PreItem
        {
            get => _preItem;

            [MemberNotNull(nameof(_preItem))]
            set => _preItem = value ?? throw new ArgumentNullException(nameof(value), "PreItem can't be null!");
        }

        /// <summary>
        /// Gets called for each item to transform it, even if it won't be passed through.
        /// </summary>
        /// <remarks>
        /// Called by the default <see cref="TransformItems">TransformItems</see>, but not directly.
        /// </remarks>
        public Func<TOriginal, Task<TTransformed>> TransformItem
        {
            get => _transformItem;

            [MemberNotNull(nameof(_transformItem))]
            set => _transformItem = value ?? throw new ArgumentNullException(nameof(value), "TransformItem can't be null!");
        }

        /// <summary>
        /// Gets called for each item to transform it into a sequence of items to return, even if it won't be passed through.
        /// </summary>
        /// <remarks>
        /// Has precedence over <see cref="TransformItem">TransformItem</see> - the default implementation just passes the call through.
        /// </remarks>
        public Func<TOriginal, IAsyncEnumerable<TTransformed>> TransformItems
        {
            get => _transformItems;

            [MemberNotNull(nameof(_transformItems))]
            set => _transformItems = value ?? throw new ArgumentNullException(nameof(value), "TransformItems can't be null!");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerable{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into and transform.</param>
        /// <param name="transformItem">The transformation function.</param>
        public AsyncEnumerableInjector(IAsyncEnumerable<TOriginal> enumerable, Func<TOriginal, Task<TTransformed>> transformItem)
            : this(enumerable.GetAsyncEnumerator(), transformItem) { }

        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerator{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into and transform.</param>
        /// <param name="transformItem">The transformation function. Called through the default <see cref="TransformItems">TransformItems</see> implementation.</param>
        public AsyncEnumerableInjector(IAsyncEnumerator<TOriginal> enumerator, Func<TOriginal, Task<TTransformed>> transformItem)
        {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            TransformItem = transformItem;
            TransformItems = DefaultTransformItemsAsync;
            PostItems = DefaultPostItemsAsync;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerable{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into and transform.</param>
        /// <param name="transformItems">The transformation function. Takes precendence over <see cref="TransformItem">TransformItem</see>.</param>
        public AsyncEnumerableInjector(IAsyncEnumerable<TOriginal> enumerable, Func<TOriginal, IAsyncEnumerable<TTransformed>> transformItems)
            : this(enumerable.GetAsyncEnumerator(), transformItems) { }

        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerator{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into and transform.</param>
        /// <param name="transformItems">The transformation function. Takes precendence over <see cref="TransformItem">TransformItem</see>.</param>
        public AsyncEnumerableInjector(IAsyncEnumerator<TOriginal> enumerator, Func<TOriginal, IAsyncEnumerable<TTransformed>> transformItems)
        {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            TransformItems = transformItems;
            PostItems = DefaultPostItemsAsync;
        }

        /// <summary>
        /// Injects into and transforms the input enumeration.
        /// </summary>
        /// <returns>The injected and transformed enumeration.</returns>
        public async IAsyncEnumerator<TTransformed> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Prefix();

            while (await _enumerator.MoveNextAsync())
            {
                var item = _enumerator.Current;
                var returnItem = await PreItem(item);
                //var transformedItems = await TransformItems(item).ToArrayAsync();
                var transformedItems = TransformItems(item).ToEnumerable().ToArray();

                if (returnItem)
                {
                    foreach (var transformedItem in transformedItems)
                        yield return transformedItem;
                }

                await PostItems(item, transformedItems, returnItem);
            }

            await Postfix();
        }

        private async Task DefaultPostItemsAsync(TOriginal original, TTransformed[] transformedItems, bool returned)
        {
            foreach (var transformedItem in transformedItems)
                await PostItem(original, transformedItem, returned);
        }

        private async IAsyncEnumerable<TTransformed> DefaultTransformItemsAsync(TOriginal original)
        {
            yield return await TransformItem(original);
        }
    }

    /// <summary>
    /// Provides the ability to inject actions into the execution of an enumeration without transforming it.<br/><br/>
    /// This example shows how to apply the <see cref="AsyncEnumerableInjector{T}"/> when patching a function.<br/>
    /// Of course you typically wouldn't patch with a generic method, that's just for illustrating the Type usage.
    /// <code>
    /// static void Postfix&lt;T&gt;(ref IEnumerable&lt;T&gt; __result)
    /// {
    ///     __result = new AsyncEnumerableInjector&lt;T&gt;(__result)
    ///     {
    ///         Prefix = () => Msg("Before the first item is returned"),
    ///         PreItem = item => { Msg("Decide if an item gets returned"); return true; },
    ///         TransformItem = item => { Msg("Change what the item is exactly"); return item; },
    ///         PostItem = (original, transformed, returned) => Msg("After control would come back to the generator after a yield return"),
    ///         Postfix = () => Msg("When the generator stopped returning items")
    ///     };
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of the enumeration's items.</typeparam>
    public class AsyncEnumerableInjector<T> : AsyncEnumerableInjector<T, T>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{T}"/> class using the supplied input <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into.</param>
        public AsyncEnumerableInjector(IAsyncEnumerable<T> enumerable)
            : this(enumerable.GetAsyncEnumerator()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="AsyncEnumerableInjector{T}"/> class using the supplied input <see cref="IAsyncEnumerator{T}"/>.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into.</param>
        public AsyncEnumerableInjector(IAsyncEnumerator<T> enumerator)
            : base(enumerator, AsyncEnumerableItemExtensions.YieldAsync) { }
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously