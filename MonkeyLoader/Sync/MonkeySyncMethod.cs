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
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, T}"/>
    public interface ILinkedMonkeySyncMethod<out TLink, out TSyncObject> : ILinkedMonkeySyncValue<TLink, TSyncObject>
        where TSyncObject : ILinkedMonkeySyncObject<TLink>
    {
    }

    /// <summary>
    /// Defines the generic interface for linked <see cref="MonkeySyncMethod{TLink, TSyncObject, T}"/>s.
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
    /// Defines the interface for not yet linked <see cref="MonkeySyncMethod{TLink, TSyncObject, T}"/>s.
    /// </summary>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, T}"/>
    public interface IUnlinkedMonkeySyncMethod<TLink, TSyncObject> : ILinkedMonkeySyncMethod<TLink, TSyncObject>, IUnlinkedMonkeySyncValue<TLink, TSyncObject>
        where TSyncObject : ILinkedMonkeySyncObject<TLink>
    { }

    /// <summary>
    /// Implements an abstract example for <see cref="ILinkedMonkeySyncMethod{TLink, T}"/>s.
    /// </summary>
    /// <remarks>
    /// This class is in all likelihood not useful to actually derive from.<br/>
    /// It mainly serves as an example for how an implementation could look.
    /// </remarks>
    /// <inheritdoc cref="ILinkedMonkeySyncMethod{TLink, T}"/>
    public abstract class MonkeySyncMethod<TLink, TSyncObject, T> : MonkeySyncValue<TLink, TSyncObject, T>,
            IUnlinkedMonkeySyncMethod<TLink, TSyncObject>, ILinkedMonkeySyncMethod<TLink, TSyncObject, T>
        where TSyncObject : class, ILinkedMonkeySyncObject<TLink>
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

        /// <summary>
        /// Creates structures that make this method triggerable by others locally or in shared environments,
        /// whether they have the mod implementing the method or not.
        /// </summary>
        public abstract void MakeInvocation();
    }
}