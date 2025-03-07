using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Represents the method that will create new instances
    /// of sync objects linking via <typeparamref name="TLink"/>.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    /// <returns>The created but not yet linked sync object.</returns>
    public delegate IUnlinkedMonkeySyncObject<TLink> SyncObjectFactory<TLink>()
        where TLink : class;

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
        [MemberNotNullWhen(true, nameof(LinkObject))]
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
        /// Establishes this sync object's link with the given object.<br/>
        /// If the link is successfully created, the now linked sync object will be
        /// <see cref="MonkeySyncRegistry.RegisterLinkedSyncObject{TLink}">added</see>
        /// to the <see cref="MonkeySyncRegistry"/>.
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
    /// The <see cref="IUnlinkedMonkeySyncValue{TLink}"/>-derived interface
    /// that the MonkeySync values of this object must implement.
    /// </typeparam>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    public abstract class MonkeySyncObject<TSyncObject, TSyncValue, TLink> : IUnlinkedMonkeySyncObject<TLink>
        where TSyncObject : MonkeySyncObject<TSyncObject, TSyncValue, TLink>
        where TSyncValue : IUnlinkedMonkeySyncValue<TLink>
        where TLink : class
    {
        /// <summary>
        /// The <see cref="MethodInfo"/>s of the detected <see cref="MonkeySyncMethodAttribute">MonkeySync
        /// methods</see> of this type by their name.
        /// </summary>
        protected static readonly Dictionary<string, MethodInfo> methodInfosByName = new(StringComparer.Ordinal);

        /// <summary>
        /// The getters for the detected <typeparamref name="TSyncValue"/> instance properties by their name.
        /// </summary>
        protected static readonly Dictionary<string, Func<TSyncObject, TSyncValue>> propertyAccessorsByName = new(StringComparer.Ordinal);

        /// <summary>
        /// The <see cref="Action"/>s to invoke the detected <see cref="MonkeySyncMethodAttribute">MonkeySync
        /// methods</see> of this type by their name.
        /// </summary>
        protected readonly Dictionary<string, Action> methodsByName = new(StringComparer.Ordinal);

        /// <summary>
        /// The <typeparamref name="TSyncValue"/> instances associated with this sync object.
        /// </summary>
        protected readonly HashSet<TSyncValue> syncValues = [];

        private bool _disposedValue;

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(LinkObject))]
        public bool HasLinkObject => LinkObject is not null;

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(LinkObject))]
        public abstract bool IsLinkValid { get; }

        /// <inheritdoc/>
        public TLink LinkObject { get; private set; } = null!;

        object IMonkeySyncObject.LinkObject => LinkObject;

        static MonkeySyncObject()
        {
            var syncValueType = typeof(TSyncValue);
            var syncValueProperties = typeof(TSyncObject).GetProperties(AccessTools.all)
                .Where(property => syncValueType.IsAssignableFrom(property.PropertyType) && (!(property.GetGetMethod()?.IsStatic ?? true)));

            foreach (var property in syncValueProperties)
                propertyAccessorsByName.Add(property.Name, (TSyncObject instance) => (TSyncValue)property.GetValue(instance));

            // Replace this with a special MonkeySyncMethod type that takes an action as the target
            // The invoke method can then be overridden to for example set the user field that triggers the method.
            var syncMethods = typeof(TSyncObject).GetMethods(AccessTools.all)
                .Where(MonkeySyncMethodAttribute.IsValid);

            foreach (var method in syncMethods)
                methodInfosByName.Add(method.Name, method);
        }

        /// <summary>
        /// Initializes a new instance of this MonkeySync object.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When <typeparamref name="TSyncObject"/> is not the type being instantiated.
        /// </exception>
        protected MonkeySyncObject()
        {
            if (GetType() != typeof(TSyncObject))
                throw new InvalidOperationException("TSyncObject must be the concrete Type being instantiated!");

            foreach (var methodEntry in methodInfosByName)
                methodsByName.Add(methodEntry.Key, AccessTools.MethodDelegate<Action>(methodEntry.Value, this));
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

            if (!EstablishLink(fromRemote))
                return false;

            MonkeySyncRegistry.RegisterLinkedSyncObject(this);
            return true;
        }

        /// <remarks><para>
        /// <i>By default:</i> Sets up the <see cref="INotifyValueChanged.Changed"/> event handlers
        /// and calls <see cref="EstablishLinkFor(TSyncValue, string, bool)">EstablishLinkFor</see>
        /// for every readable <typeparamref name="TSyncValue"/> instance property and
        /// <see cref="EstablishLinkFor(TSyncValue, string, bool)">its overload</see> for every
        /// <see cref="MonkeySyncMethodAttribute">MonkeySync method</see> on <typeparamref name="TSyncObject"/>.
        /// </para><para>
        /// The detected properties are stored in <see cref="propertyAccessorsByName">propertyAccessorsByName</see>,
        /// while the detected methods are stored in <see cref="methodInfosByName">methodsByName</see>.
        /// </para><para>
        /// This method is called by <see cref="LinkWith">LinkWith</see>
        /// after the <see cref="LinkObject">LinkObject</see> has been assigned.<br/>
        /// It should ensure that a link object created from the remote side
        /// is handled appropriately and without duplications as well.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="LinkWith"/>
        protected virtual bool EstablishLink(bool fromRemote)
        {
            var success = true;

            foreach (var syncValueProperty in propertyAccessorsByName)
            {
                var syncValue = syncValueProperty.Value((TSyncObject)this);

                syncValue.Changed += (sender, changedArgs)
                    => OnPropertyChanged(syncValueProperty.Key);

                success &= EstablishLinkFor(syncValue, syncValueProperty.Key, fromRemote);
            }

            foreach (var syncMethod in methodsByName)
                success &= EstablishLinkFor(syncMethod.Value, syncMethod.Key, fromRemote);

            return success;
        }

        /// <summary>
        /// Creates a link for the given sync value of the given name.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Adds the given sync value to the <see cref="syncValues">set of instances</see> and calls
        /// <c><paramref name="syncValue"/>.<see cref="IUnlinkedMonkeySyncValue{TLink}.EstablishLinkFor">EstablishLinkFor</see>(…)</c>.
        /// </remarks>
        /// <param name="syncValue">The sync value to link.</param>
        /// <param name="propertyName">The name of the sync value to link.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected virtual bool EstablishLinkFor(TSyncValue syncValue, string propertyName, bool fromRemote)
        {
            syncValues.Add(syncValue);
            return syncValue.EstablishLinkFor(this, propertyName, fromRemote);
        }

        /// <summary>
        /// Creates a link for the given sync method of the given name.
        /// </summary>
        /// <remarks>
        /// Any <typeparamref name="TSyncValue"/>s created for this
        /// must be added to the <see cref="syncValues">set of instances</see>.
        /// </remarks>
        /// <param name="syncMethod">The sync method to link.</param>
        /// <param name="methodName">The name of the sync method to link.</param>
        /// <param name="fromRemote">Whether the link is being established from the remote side.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected abstract bool EstablishLinkFor(Action syncMethod, string methodName, bool fromRemote);

        /// <summary>
        /// Cleans up any managed resources as part of <see cref="Dispose()">disposing</see>.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Disposes all <typeparamref name="TSyncValue"/> instances
        /// that were added to the <see cref="syncValues">set of instances</see>,
        /// and the <see cref="LinkObject">LinkObject</see> if it's <see cref="IDisposable"/>.
        /// </remarks>
        protected virtual void OnDisposing()
        {
            foreach (var syncValue in syncValues)
                syncValue.Dispose();

            if (LinkObject is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Cleans up any unmanaged resources as part of
        /// <see cref="Dispose()">disposing</see> or finalization.
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

            try
            {
                Invalidated.TryInvokeAll();
            }
            catch { }

            Dispose();
        }

        /// <summary>
        /// Triggers the <see cref="PropertyChanged">PropertyChanged</see>
        /// event with the given <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        /// This is automatically called for any <typeparamref name="TSyncValue"/> properties.
        /// </remarks>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            var eventData = new PropertyChangedEventArgs(propertyName);

            PropertyChanged?.Invoke(this, eventData);
        }

        /// <remarks><para>
        /// <i>By default:</i> Calls <see cref="TryRestoreLinkFor(TSyncValue)">TryRestoreLinkFor</see>
        /// for every readable <typeparamref name="TSyncValue"/> instance property and
        /// <see cref="TryRestoreLinkFor(Action, string)">its overload</see> for every
        /// <see cref="MonkeySyncMethodAttribute">MonkeySync method</see> on <typeparamref name="TSyncObject"/>.<br/>
        /// The detected properties are stored in <see cref="propertyAccessorsByName">propertyAccessorsByName</see>,
        /// while the detected methods are stored in <see cref="methodInfosByName">methodsByName</see>.
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

            foreach (var syncValues in propertyAccessorsByName.Values)
                success &= TryRestoreLinkFor(syncValues((TSyncObject)this));

            foreach (var syncMethod in methodsByName)
                success &= TryRestoreLinkFor(syncMethod.Value, syncMethod.Key);

            return success;
        }

        /// <summary>
        /// Tries to restore the link for the given sync value.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Calls
        /// <c><paramref name="syncValue"/>.<see cref="ILinkedMonkeySyncValue{TLink}.TryRestoreLink">TryRestoreLink</see>()</c>.
        /// </remarks>
        /// <param name="syncValue">The sync value to link.</param>
        /// <returns><c>true</c> if the link was successfully restored; otherwise, <c>false</c>.</returns>
        protected virtual bool TryRestoreLinkFor(TSyncValue syncValue)
            => syncValue.TryRestoreLink();

        /// <summary>
        /// Tries to restore the link for the given sync method of the given name.
        /// </summary>
        /// <param name="syncMethod">The sync method to link.</param>
        /// <param name="methodName">The name of the sync method to link.</param>
        /// <returns><c>true</c> if the link was successfully restored; otherwise, <c>false</c>.</returns>
        protected abstract bool TryRestoreLinkFor(Action syncMethod, string methodName);

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
        /// <param name="syncObject">The sync object that got invalidated.</param>
        public delegate void InvalidatedHandler(TSyncObject syncObject);

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}