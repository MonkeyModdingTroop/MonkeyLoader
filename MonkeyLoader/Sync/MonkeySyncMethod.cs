using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// The delegate that is represented by MonkeySync methods.
    /// </summary>
    public delegate void MonkeySyncAction();

    /// <summary>
    /// The delegate that is represented by MonkeySync methods.
    /// </summary>
    /// <param name="trigger">The value that triggered this method.</param>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, T}"/>
    public delegate void MonkeySyncFunc<T>(T trigger);

    /// <summary>
    /// Defines the non-generic interface for <see cref="ILinkedMonkeySyncMethod{TLink, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, TLinkedSyncValue, T}"/>
    public interface ILinkedMonkeySyncMethod<out TLink, out TSyncObject> : ILinkedMonkeySyncValue<TLink, TSyncObject>
        where TSyncObject : ILinkedMonkeySyncObject<TLink>
    {
    }

    /// <summary>
    /// Defines the generic interface for linked <see cref="MonkeySyncMethod{TLink, TSyncObject, TLinkedSyncValue, T}"/>s.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object that this sync value links to.</typeparam>
    /// <typeparam name="TSyncObject">The type of the sync object that may contain this sync value.</typeparam>
    /// <typeparam name="T">The type of the <see cref="ILinkedMonkeySyncValue{T, TSyncObject}.Value">Value</see> that can trigger this sync method.</typeparam>
    public interface ILinkedMonkeySyncMethod<out TLink, out TSyncObject, T> : ILinkedMonkeySyncMethod<TLink, TSyncObject>, ILinkedMonkeySyncValue<TLink, TSyncObject, T>
        where TSyncObject : ILinkedMonkeySyncObject<TLink>
    {
        /// <summary>
        /// Gets the delegate that can be triggered by this sync method.
        /// </summary>
        public MonkeySyncFunc<T> Function { get; }
    }

    /// <summary>
    /// Defines the interface for not yet linked <see cref="MonkeySyncMethod{TLink, TSyncObject, TLinkedSyncValue, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, TLinkedSyncValue, T}"/>
    public interface IUnlinkedMonkeySyncMethod<in TLink, in TSyncObject, out TLinkedSyncValue> : IUnlinkedMonkeySyncValue<TLink, TSyncObject, TLinkedSyncValue>
        where TSyncObject : ILinkedMonkeySyncObject<TLink>
        where TLinkedSyncValue : ILinkedMonkeySyncValue<TLink, TSyncObject>
    { }

    /// <summary>
    /// Implements an abstract example for <see cref="ILinkedMonkeySyncMethod{TLink, T}"/>s.
    /// </summary>
    /// <remarks>
    /// This class is in all likelihood not useful to actually derive from.<br/>
    /// It mainly serves as an example for how an implementation could look.
    /// </remarks>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, TLinkedSyncValue, T}"/>
    public abstract class MonkeySyncMethod<TLink, TSyncObject, TSyncValue, T> : MonkeySyncValue<TLink, TSyncObject, TSyncValue, T>,
            IUnlinkedMonkeySyncMethod<TLink, TSyncObject, TSyncValue>,
            ILinkedMonkeySyncMethod<TLink, TSyncObject, T>
        where TSyncObject : class, ILinkedMonkeySyncObject<TLink>
        where TSyncValue : MonkeySyncMethod<TLink, TSyncObject, TSyncValue, T>
    {
        /// <inheritdoc/>
        public MonkeySyncFunc<T> Function { get; }

        /// <summary>
        /// Creates a new sync method instance that wraps the given <paramref name="value"/>,
        /// changes of which can trigger the target <paramref name="function"/>.
        /// </summary>
        /// <param name="function">The delegate that can be triggered by this sync method.</param>
        /// <param name="value">The value to wrap.</param>
        protected MonkeySyncMethod(MonkeySyncFunc<T> function, T value) : base(value)
        {
            Function = function;
        }

        /// <summary>
        /// Creates a new sync method instance that wraps the given <paramref name="value"/>,
        /// changes of which can trigger the target <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The delegate that can be triggered by this sync method.</param>
        /// <param name="value">The value to wrap.</param>
        protected MonkeySyncMethod(MonkeySyncAction action, T value)
            : this(_ => action(), value)
        { }

        TSyncValue? IUnlinkedMonkeySyncValue<TLink, TSyncObject, TSyncValue>.EstablishLinkFor(TSyncObject syncObject, string name, bool fromRemote)
            => EstablishLinkFor(syncObject, name, fromRemote);

        /// <summary>
        /// Creates structures that make this method triggerable by others locally or in shared environments,
        /// whether they have the mod implementing the method or not.
        /// </summary>
        public abstract void MakeInvocation();
    }
}