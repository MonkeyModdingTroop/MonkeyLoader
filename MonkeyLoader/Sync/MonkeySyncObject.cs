using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Defines the generic interface for <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValues, TLink}">MonkeySync objects</see> that have been linked.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    public interface ILinkedMonkeySyncObject<out TLink> : IMonkeySyncObject
    {
        /// <inheritdoc cref="IMonkeySyncObject.LinkObject"/>
        public new TLink LinkObject { get; }
    }

    /// <summary>
    /// Defines the non-generic interface for <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValues, TLink}">MonkeySync objects</see>.
    /// </summary>
    public interface IMonkeySyncObject : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Gets whether this sync object has a <see cref="LinkObject">link object</see>.
        /// </summary>
        [MemberNotNullWhen(true, nameof(LinkObject))]
        public bool HasLinkObject { get; }

        /// <summary>
        /// Gets whether this sync object has a valid link.
        /// </summary>
        public bool IsLinkValid { get; }

        /// <summary>
        /// Gets the link object used by this sync object.
        /// </summary>
        public object LinkObject { get; }
    }

    /// <summary>
    /// Defines the generic interface for <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValues, TLink}">MonkeySync objects</see> that are yet to be linked.
    /// </summary>
    /// <inheritdoc/>
    public interface IUnlinkedMonkeySyncObject<TLink> : ILinkedMonkeySyncObject<TLink>
        where TLink : class
    {
        /// <summary>
        /// Establishes this sync object's link with the given object.
        /// </summary>
        /// <remarks>
        /// If the link fails or gets broken, a new instance has to be created.
        /// </remarks>
        /// <param name="linkObject">The link object to be used by this sync object.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the established link is valid; otherwise, <c>false</c>.</returns>
        public bool LinkWith(TLink linkObject, bool fromRemote = false);
    }

    /// <summary>
    /// Implements the abstract base for MonkeySync objects.
    /// </summary>
    /// <typeparam name="TSyncObject">The concrete type of the MonkeySync object.</typeparam>
    /// <typeparam name="TSyncValue">
    /// The <see cref="IMonkeySyncValue"/>-derived interface
    /// that the MonkeySync values of this object must implement.
    /// </typeparam>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    public abstract class MonkeySyncObject<TSyncObject, TSyncValue, TLink> : IUnlinkedMonkeySyncObject<TLink>
        where TSyncObject : MonkeySyncObject<TSyncObject, TSyncValue, TLink>
        where TSyncValue : IMonkeySyncValue
        where TLink : class
    {
        /// <summary>
        /// The detected <see cref="MonkeySyncMethodAttribute">MonkeySync nethods</see> by their name.
        /// </summary>
        protected static readonly Dictionary<string, Action<TSyncObject>> methodsByName = new(StringComparer.Ordinal);

        /// <summary>
        /// The getters for the detected <typeparamref name="TSyncValue"/> instance properties by their name.
        /// </summary>
        protected static readonly Dictionary<string, Func<TSyncObject, TSyncValue>> propertyAccessorsByName = new(StringComparer.Ordinal);

        private bool _disposedValue;

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(LinkObject))]
        public bool HasLinkObject => LinkObject is not null;

        /// <inheritdoc/>
        public abstract bool IsLinkValid { get; }

        /// <inheritdoc/>
        public TLink LinkObject { get; private set; } = null!;

        object IMonkeySyncObject.LinkObject => LinkObject;

        static MonkeySyncObject()
        {
            var syncValueType = typeof(TSyncValue);
            var syncValueProperties = AccessTools.GetDeclaredProperties(typeof(TSyncObject))
                .Where(property => syncValueType.IsAssignableFrom(property.PropertyType) && (!(property.GetGetMethod()?.IsStatic ?? true)));

            foreach (var property in syncValueProperties)
                propertyAccessorsByName.Add(property.Name, (TSyncObject instance) => (TSyncValue)property.GetValue(instance));

            var syncMethods = AccessTools.GetDeclaredMethods(typeof(TSyncObject))
                .Where(method => !method.IsStatic && !method.ContainsGenericParameters && method.ReturnType == typeof(void) && method.GetParameters().Length == 0);

            foreach (var method in syncMethods)
                methodsByName.Add(method.Name, (TSyncObject instance) => method.Invoke(instance, null));
        }

        /// <summary>
        /// Ensures any unmanaged resources are <see cref="Dispose(bool)">disposed</see>.
        /// </summary>
        ~MonkeySyncObject()
        {
            Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in the 'OnDisposing()' or 'OnFinalizing()' methods
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public bool LinkWith(TLink linkObject, bool fromRemote = false)
        {
            if (HasLinkObject)
                throw new InvalidOperationException("Can only assign a link object once!");

            LinkObject = linkObject;

            return EstablishLinkWith(linkObject, fromRemote);
        }

        /// <summary>
        /// Creates a link for the given sync value of the given name.
        /// </summary>
        /// <param name="propertyName">The name of the sync value to link.</param>
        /// <param name="syncValue">The sync value to link.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected abstract bool EstablishLinkFor(string propertyName, TSyncValue syncValue, bool fromRemote);

        /// <summary>
        /// Creates a link for the given sync method of the given name.
        /// </summary>
        /// <param name="methodName">The name of the sync method to link.</param>
        /// <param name="syncMethod">The sync method to link.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected abstract bool EstablishLinkFor(string methodName, Action<TSyncObject> syncMethod, bool fromRemote);

        /// <remarks><para>
        /// <i>By default:</i> Sets up the <see cref="INotifyValueChanged.Changed"/> event handlers
        /// and calls <see cref="EstablishLinkFor(string, TSyncValue, bool)">EstablishLinkFor</see>
        /// for every readable <typeparamref name="TSyncValue"/> instance property and
        /// <see cref="EstablishLinkFor(string, TSyncValue, bool)">its overload</see> for every
        /// <see cref="MonkeySyncMethodAttribute">MonkeySync method</see> on <typeparamref name="TSyncObject"/>.<br/>
        /// The detected properties are stored in <see cref="propertyAccessorsByName">propertyAccessorsByName</see>,
        /// while the detected methods are stored in <see cref="methodsByName">methodsByName</see>.
        /// </para><para>
        /// This method is called by <see cref="LinkWith">LinkWith</see>
        /// after the <see cref="LinkObject">LinkObject</see> has been assigned.<br/>
        /// It should ensure that a link object created from the remote side
        /// is handled appropriately and without duplications as well.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="LinkWith"/>
        protected virtual bool EstablishLinkWith(TLink linkObject, bool fromRemote)
        {
            var success = true;

            foreach (var syncValueProperty in propertyAccessorsByName)
            {
                var syncValue = syncValueProperty.Value((TSyncObject)this);

                syncValue.Changed += (sender, changedArgs)
                    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(syncValueProperty.Key));

                success &= EstablishLinkFor(syncValueProperty.Key, syncValue, fromRemote);
            }

            foreach (var syncMethod in methodsByName)
                success &= EstablishLinkFor(syncMethod.Key, syncMethod.Value, fromRemote);

            return success;
        }

        /// <summary>
        /// Cleans up any managed resources as part of <see cref="Dispose()">disposing</see>.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Disposes the <see cref="LinkObject">LinkObject</see> if it's <see cref="IDisposable"/>.
        /// </remarks>
        protected virtual void OnDisposing()
        {
            if (LinkObject is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Cleans up any unmanaged resources as part of
        /// <see cref="Dispose()">disposing</see> or <see cref="~MonkeySyncObject()"/>finalization.
        /// </summary>
        protected virtual void OnFinalizing()
        { }

        /// <summary>
        /// <see cref="TryRestoreLink">Tries to restore the link</see> when it becomes invalidated
        /// and triggers the <see cref="Invalidated">Invalidated</see> when that fails.<br/>
        /// Afterwards, the object is automatically <see cref="Dispose()">disposed</see>.
        /// </summary>
        /// <remarks>
        /// Should be called from a derived class when something happens
        /// that makes <see cref="IsLinkValid">IsLinkValid</see> <c>false</c>.
        /// </remarks>
        protected void OnLinkInvalidated()
        {
            if (!IsLinkValid && TryRestoreLink() && IsLinkValid)
                return;

            Invalidated.TryInvokeAll();

            Dispose();
        }

        /// <summary>
        /// Triggers the <see cref="PropertyChanged">PropertyChanged</see>
        /// event with the given <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        /// This is automatically called for any <see cref="MonkeySyncValue{T}"/> properties.
        /// </remarks>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            // Still needs to be hooked up somewhere
            var eventData = new PropertyChangedEventArgs(propertyName);

            PropertyChanged?.Invoke(this, eventData);
        }

        /// <remarks><para>
        /// <i>By default:</i> Calls <see cref="TryRestoreLinkFor(string, TSyncValue)">TryRestoreLinkFor</see>
        /// for every readable <typeparamref name="TSyncValue"/> instance property and
        /// <see cref="TryRestoreLinkFor(string, TSyncValue)">its overload</see> for every
        /// <see cref="MonkeySyncMethodAttribute">MonkeySync method</see> on <typeparamref name="TSyncObject"/>.<br/>
        /// The detected properties are stored in <see cref="propertyAccessorsByName">propertyAccessorsByName</see>,
        /// while the detected methods are stored in <see cref="methodsByName">methodsByName</see>.
        /// </para><para>
        /// This method is called by <see cref="OnLinkInvalidated">OnLinkInvalidated</see>
        /// if <see cref="IsLinkValid">IsLinkValid</see> has become <c>false</c>.<br/>
        /// It should ensure that any still valid links are
        /// handled appropriately and without duplications as well.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="OnLinkInvalidated"/>
        protected virtual bool TryRestoreLink()
        {
            var success = true;

            foreach (var syncValueProperty in propertyAccessorsByName)
                success &= TryRestoreLinkFor(syncValueProperty.Key, syncValueProperty.Value((TSyncObject)this));

            foreach (var syncMethod in methodsByName)
                success &= TryRestoreLinkFor(syncMethod.Key, syncMethod.Value);

            return success;
        }

        /// <summary>
        /// Tries to restore the link for the given sync value of the given name.
        /// </summary>
        /// <param name="propertyName">The name of the sync value to link.</param>
        /// <param name="syncValue">The sync value to link.</param>
        /// <returns><c>true</c> if the link was successfully restored; otherwise, <c>false</c>.</returns>
        protected abstract bool TryRestoreLinkFor(string propertyName, TSyncValue syncValue);

        /// <summary>
        /// Tries to restore the link for the given sync method of the given name.
        /// </summary>
        /// <param name="methodName">The name of the sync method to link.</param>
        /// <param name="syncMethod">The sync method to link.</param>
        /// <returns><c>true</c> if the link was successfully restored; otherwise, <c>false</c>.</returns>
        protected abstract bool TryRestoreLinkFor(string methodName, Action<TSyncObject> syncMethod);

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
                OnDisposing();

            OnFinalizing();

            _disposedValue = true;
        }

        /// <summary>
        /// Occurs when <see cref="IsLinkValid">IsLinkValid</see> becomes <c>false</c>
        /// and it could not be <see cref="TryRestoreLink">restored</see>.
        /// </summary>
        public event InvalidatedHandler? Invalidated;

        /// <summary>
        /// Represents the method that will handle the <see cref="Invalidated">Invalidated</see>
        /// event raised when <see cref="IsLinkValid">IsLinkValid</see> becomes <c>false</c>
        /// and it could not be <see cref="TryRestoreLink">restored</see>.
        /// </summary>
        /// <param name="syncObject"></param>
        public delegate void InvalidatedHandler(TSyncObject syncObject);

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}