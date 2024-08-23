using EnumerableToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonkeyLoader.Utility
{
    /// <summary>
    /// Provides the ability to inject actions into the execution of an enumeration while transforming it.<br/><br/>
    /// This example shows how to apply the <see cref="EnumerableInjector{TIn, TOut}"/> when patching a function.<br/>
    /// Of course you typically wouldn't patch with a generic method, that's just for illustrating the Type usage.
    /// <code>
    /// private static void Postfix&lt;Original, Transformed&gt;(ref IEnumerable&lt;Original&gt; __result) where Transformed : Original
    /// {
    ///     __result = new EnumerableInjector&lt;Original, Transformed&gt;(__result,
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
    public class EnumerableInjector<TOriginal, TTransformed> : IEnumerable<TTransformed> where TTransformed : TOriginal
    {
        /// <summary>
        /// Internal enumerator for iteration.
        /// </summary>
        private readonly IEnumerator<TOriginal> _enumerator;

        private Action _postfix = () => { };
        private Action<TOriginal, TTransformed, bool> _postItem = (original, transformed, returned) => { };
        private Action<TOriginal, TTransformed[], bool> _postItems;
        private Action _prefix = () => { };
        private Func<TOriginal, bool> _preItem = item => true;
        private Func<TOriginal, TTransformed> _transformItem = item => throw new NotImplementedException("You're supposed to insert your own transformation function here when not using TransformItems!");
        private Func<TOriginal, IEnumerable<TTransformed>> _transformItems;

        /// <summary>
        /// Gets called when the wrapped enumeration returned the last item.
        /// </summary>
        public Action Postfix
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
        public Action<TOriginal, TTransformed, bool> PostItem
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
        public Action<TOriginal, TTransformed[], bool> PostItems
        {
            get => _postItems;

            [MemberNotNull(nameof(_postItems))]
            set => _postItems = value ?? throw new ArgumentNullException(nameof(value), "PostItems can't be null!");
        }

        /// <summary>
        /// Gets called before the enumeration returns the first item.
        /// </summary>
        public Action Prefix
        {
            get => _prefix;

            [MemberNotNull(nameof(_prefix))]
            set => _prefix = value ?? throw new ArgumentNullException(nameof(value), "Prefix can't be null!");
        }

        /// <summary>
        /// Gets called for each item to determine whether it should be passed through.
        /// </summary>
        public Func<TOriginal, bool> PreItem
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
        public Func<TOriginal, TTransformed> TransformItem
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
        public Func<TOriginal, IEnumerable<TTransformed>> TransformItems
        {
            get => _transformItems;

            [MemberNotNull(nameof(_transformItems))]
            set => _transformItems = value ?? throw new ArgumentNullException(nameof(value), "TransformItems can't be null!");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerable{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into and transform.</param>
        /// <param name="transformItem">The transformation function.</param>
        public EnumerableInjector(IEnumerable<TOriginal> enumerable, Func<TOriginal, TTransformed> transformItem)
            : this(enumerable.GetEnumerator(), transformItem) { }

        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerator{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into and transform.</param>
        /// <param name="transformItem">The transformation function. Called through the default <see cref="TransformItems">TransformItems</see> implementation.</param>
        public EnumerableInjector(IEnumerator<TOriginal> enumerator, Func<TOriginal, TTransformed> transformItem)
        {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            TransformItem = transformItem;
            TransformItems = DefaultTransformItems;
            PostItems = DefaultPostItems;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerable{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into and transform.</param>
        /// <param name="transformItems">The transformation function. Takes precendence over <see cref="TransformItem">TransformItem</see>.</param>
        public EnumerableInjector(IEnumerable<TOriginal> enumerable, Func<TOriginal, IEnumerable<TTransformed>> transformItems)
            : this(enumerable.GetEnumerator(), transformItems) { }

        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{TIn, TOut}"/> class using the supplied input <see cref="IEnumerator{T}"/> and transform function.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into and transform.</param>
        /// <param name="transformItems">The transformation function. Takes precendence over <see cref="TransformItem">TransformItem</see>.</param>
        public EnumerableInjector(IEnumerator<TOriginal> enumerator, Func<TOriginal, IEnumerable<TTransformed>> transformItems)
        {
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            TransformItems = transformItems;
            PostItems = DefaultPostItems;
        }

        /// <summary>
        /// Injects into and transforms the input enumeration.
        /// </summary>
        /// <returns>The injected and transformed enumeration.</returns>
        public IEnumerator<TTransformed> GetEnumerator()
        {
            Prefix();

            while (_enumerator.MoveNext())
            {
                var item = _enumerator.Current;
                var returnItem = PreItem(item);
                var transformedItems = TransformItems(item).ToArray();

                if (returnItem)
                {
                    foreach (var transformedItem in transformedItems)
                        yield return transformedItem;
                }

                PostItems(item, transformedItems, returnItem);
            }

            Postfix();
        }

        /// <summary>
        /// Injects into and transforms the input enumeration without a generic type.
        /// </summary>
        /// <returns>The injected and transformed enumeration without a generic type.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void DefaultPostItems(TOriginal original, TTransformed[] transformedItems, bool returned)
        {
            foreach (var transformedItem in transformedItems)
                PostItem(original, transformedItem, returned);
        }

        private IEnumerable<TTransformed> DefaultTransformItems(TOriginal original)
            => TransformItem(original).Yield();
    }

    /// <summary>
    /// Provides the ability to inject actions into the execution of an enumeration without transforming it.<br/><br/>
    /// This example shows how to apply the <see cref="EnumerableInjector{T}"/> when patching a function.<br/>
    /// Of course you typically wouldn't patch with a generic method, that's just for illustrating the Type usage.
    /// <code>
    /// static void Postfix&lt;T&gt;(ref IEnumerable&lt;T&gt; __result)
    /// {
    ///     __result = new EnumerableInjector&lt;T&gt;(__result)
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
    public class EnumerableInjector<T> : EnumerableInjector<T, T>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{T}"/> class using the supplied input <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to inject into.</param>
        public EnumerableInjector(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator()) { }

        /// <summary>
        /// Creates a new instance of the <see cref="EnumerableInjector{T}"/> class using the supplied input <see cref="IEnumerator{T}"/>.
        /// </summary>
        /// <param name="enumerator">The enumerator to inject into.</param>
        public EnumerableInjector(IEnumerator<T> enumerator)
            : base(enumerator, EnumerableItemExtensions.Yield) { }
    }
}