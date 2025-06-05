using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Handles globally registering <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValue, TLink}">MonkeySync objects</see> for particular link types,
    /// so that the link implementations can create and reference them.
    /// </summary>
    public static class MonkeySyncRegistry
    {
        /// <summary>
        /// Gets the <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> linked with the given link object.
        /// </summary>
        /// <returns>The found <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see>.</returns>
        /// <exception cref="KeyNotFoundException">When no sync object is registered for the link object.</exception>
        /// <inheritdoc cref="TryGetLinkedSyncObject{TLink}"/>
        public static ILinkedMonkeySyncObject<TLink> GetLinkedSyncObject<TLink>(TLink linkObject)
            where TLink : class
        {
            if (Library<TLink>.TryGetLinkedSyncObject(linkObject, out var syncObject))
                return syncObject;

            throw new KeyNotFoundException("No sync object found for the given link object!");
        }

        /// <summary>
        /// Gets the <see cref="MonkeySyncObjectRegistration{TLink}"/> for the given <paramref name="syncObjectType"/>.
        /// </summary>
        /// <param name="syncObjectType">The type of registered sync object.</param>
        /// <inheritdoc cref="GetSyncObjectRegistration{TLink}(string)"/>
        public static MonkeySyncObjectRegistration<TLink> GetSyncObjectRegistration<TLink>(Type syncObjectType)
                where TLink : class
            => Library<TLink>.GetSyncObjectRegistration(syncObjectType);

        /// <summary>
        /// Gets the <see cref="MonkeySyncObjectRegistration{TLink}"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The name of the registered sync object.</param>
        /// <returns>The found <see cref="MonkeySyncObjectRegistration{TLink}"/>.</returns>
        /// <exception cref="KeyNotFoundException">When no sync object with the given <paramref name="name"/> has been registered.</exception>
        public static MonkeySyncObjectRegistration<TLink> GetSyncObjectRegistration<TLink>(string name)
                where TLink : class
            => Library<TLink>.GetSyncObjectRegistration(name);

        /// <summary>
        /// Determines whether there is a registered <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> that is linked to the given <see cref="ILinkedMonkeySyncObject{TLink}.LinkObject">link object</see>.
        /// </summary>
        /// <inheritdoc cref="TryGetLinkedSyncObject{TLink}"/>
        public static bool HasLinkedSyncObject<TLink>(TLink linkObject)
                where TLink : class
            => Library<TLink>.TryGetLinkedSyncObject(linkObject, out _);

        /// <summary>
        /// Registers a linked <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> so that it can be <see cref="TryGetLinkedSyncObject{TLink}">accessed</see>
        /// through a reference to its <see cref="ILinkedMonkeySyncObject{TLink}.LinkObject">link object</see>.
        /// </summary>
        /// <remarks>
        /// The link is held in a <see cref="ConditionalWeakTable{TKey, TValue}"/> -
        /// as such, the sync object may be garbage collected when the link object is.
        /// </remarks>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="syncObject">The linked sync object to register.</param>
        public static void RegisterLinkedSyncObject<TLink>(ILinkedMonkeySyncObject<TLink> syncObject)
                where TLink : class
            => Library<TLink>.RegisterLinkedSyncObject(syncObject);

        /// <summary>
        /// Registers a <see cref="MonkeySyncObject{TSyncObject, TSyncValue,
        /// TLink}">MonkeySync object</see> type with the given details for it.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The <typeparamref name="TLink"/>-unique name for the sync object type.</param>
        /// <param name="syncObjectType">The type of the sync object.</param>
        /// <param name="createSyncObject">A factory method that creates new instances of this sync object type.</param>
        /// <returns>The data of the newly registered sync object type.</returns>
        /// <exception cref="ArgumentException">
        /// When the <paramref name="syncObjectType"/> or one with the same
        /// <paramref name="name"/> has already been registered.
        /// </exception>
        public static MonkeySyncObjectRegistration<TLink> RegisterSyncObject<TLink>(string name, Type syncObjectType, SyncObjectFactory<TLink> createSyncObject)
                where TLink : class
            => Library<TLink>.RegisterSyncObject(name, syncObjectType, createSyncObject);

        /// <summary>
        /// Tries to get the <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> linked with the given link object.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="linkObject">The link object used by the sync object.</param>
        /// <param name="syncObject">The found <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see>; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if a sync object is registered for the given link object; otherwise, <c>false</c>.</returns>
        public static bool TryGetLinkedSyncObject<TLink>(TLink linkObject, [NotNullWhen(true)] out ILinkedMonkeySyncObject<TLink>? syncObject)
                where TLink : class
            => Library<TLink>.TryGetLinkedSyncObject(linkObject, out syncObject);

        /// <summary>
        /// Tries to get the <see cref="MonkeySyncObjectRegistration{TLink}"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The name of the registered sync object.</param>
        /// <param name="registeredSyncObject">The found <see cref="MonkeySyncObjectRegistration{TLink}"/>; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the registered sync object type was found; otherwise, <c>false</c>.</returns>
        public static bool TryGetSyncObjectRegistration<TLink>(string name, [NotNullWhen(true)] out MonkeySyncObjectRegistration<TLink>? registeredSyncObject)
                where TLink : class
            => Library<TLink>.TryGetSyncObjectRegistration(name, out registeredSyncObject);

        /// <summary>
        /// Tries to get the <see cref="MonkeySyncObjectRegistration{TLink}"/> for the given <paramref name="syncObjectType"/>.
        /// </summary>
        /// <param name="syncObjectType">The type of the sync object.</param>
        /// <param name="registeredSyncObject">The found <see cref="MonkeySyncObjectRegistration{TLink}"/>; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the registered sync object type was found; otherwise, <c>false</c>.</returns>
        public static bool TryGetSyncObjectRegistration<TLink>(Type syncObjectType, [NotNullWhen(true)] out MonkeySyncObjectRegistration<TLink>? registeredSyncObject)
                where TLink : class
            => Library<TLink>.TryGetSyncObjectRegistration(syncObjectType, out registeredSyncObject);

        /// <summary>
        /// Unregisters a linked <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> so that it can't be <see cref="TryGetLinkedSyncObject{TLink}">accessed</see>
        /// through a reference to its <see cref="ILinkedMonkeySyncObject{TLink}.LinkObject">link object</see> anymore.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="syncObject">The linked sync object to unregister.</param>
        public static bool UnregisterLinkedSyncObject<TLink>(ILinkedMonkeySyncObject<TLink> syncObject)
                where TLink : class
            => Library<TLink>.UnregisterLinkedSyncObject(syncObject);

        /// <summary>
        /// Removes the registered <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
        /// object</see> that was linked to the given <see cref="ILinkedMonkeySyncObject{TLink}.LinkObject">link object</see>,
        /// so that it can't be accessed through a reference to it anymore.<br/>
        /// Optionally <see cref="IDisposable.Dispose">disposes</see> of the sync object if necessary.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="linkObject">The link object used by the sync object.</param>
        /// <param name="dispose">Whether to dispose the <see cref="IDisposable.Dispose">dispose</see> of the sync object if necessary.</param>
        /// <returns><c>true</c> if a sync object was registered for the given link object; otherwise, <c>false</c>.</returns>
        public static bool UnregisterLinkedSyncObject<TLink>(TLink linkObject, bool dispose = false)
                where TLink : class
            => Library<TLink>.UnregisterLinkedSyncObject(linkObject, dispose);

        /// <summary>
        /// Removes the sync object type with the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The name of the registered sync object.</param>
        /// <returns>
        /// <c>true</c> if the registered sync object type with the given <paramref name="name"/> was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// When there's a registered sync object type with the given <paramref name="name"/>,
        /// but none with the associated <see cref="Type"/> - or there's a <see cref="MonkeySyncObjectRegistration{TLink}.Name">Name</see> mismatch.
        /// </exception>
        public static bool UnregisterSyncObject<TLink>(string name)
                where TLink : class
            => Library<TLink>.UnregisterSyncObject(name);

        /// <summary>
        /// Removes the sync object type for the given <paramref name="syncObjectType"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="syncObjectType">The type of the registered sync object.</param>
        /// <returns>
        /// <c>true</c> if the registered sync object type for the given <paramref name="syncObjectType"/> was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// When there's a registered sync object type for the given <paramref name="syncObjectType"/>,
        /// but none with the associated Name - or there's a <see cref="MonkeySyncObjectRegistration{TLink}.SyncObjectType">Type</see> mismatch.
        /// </exception>
        public static bool UnregisterSyncObject<TLink>(Type syncObjectType)
                where TLink : class
            => Library<TLink>.UnregisterSyncObject(syncObjectType);

        private static class Library<TLink>
            where TLink : class
        {
            private static readonly Dictionary<Type, MonkeySyncObjectRegistration<TLink>> _registeredObjectByType = [];
            private static readonly Dictionary<string, MonkeySyncObjectRegistration<TLink>> _registeredObjectsByName = new(StringComparer.Ordinal);
            private static readonly ConditionalWeakTable<TLink, ILinkedMonkeySyncObject<TLink>> _syncObjectsByLinkObject = new();

            public static MonkeySyncObjectRegistration<TLink> GetSyncObjectRegistration(Type type)
                => _registeredObjectByType[type];

            public static MonkeySyncObjectRegistration<TLink> GetSyncObjectRegistration(string name)
                => _registeredObjectsByName[name];

            public static void RegisterLinkedSyncObject(ILinkedMonkeySyncObject<TLink> syncObject)
                => _syncObjectsByLinkObject.Add(syncObject.LinkObject, syncObject);

            public static MonkeySyncObjectRegistration<TLink> RegisterSyncObject(string name, Type syncObjectType, SyncObjectFactory<TLink> createSyncObject)
            {
                if (_registeredObjectsByName.ContainsKey(name) || _registeredObjectByType.ContainsKey(syncObjectType))
                    throw new ArgumentException($"Sync Object type [{syncObjectType.CompactDescription()}] with name [{name}] has already been registered!");

                var registeredObject = new MonkeySyncObjectRegistration<TLink>(name, syncObjectType, createSyncObject);

                _registeredObjectsByName.Add(name, registeredObject);
                _registeredObjectByType.Add(syncObjectType, registeredObject);

                return registeredObject;
            }

            public static bool TryGetLinkedSyncObject(TLink linkObject, [NotNullWhen(true)] out ILinkedMonkeySyncObject<TLink>? syncObject)
                => _syncObjectsByLinkObject.TryGetValue(linkObject, out syncObject);

            public static bool TryGetSyncObjectRegistration(string name, [NotNullWhen(true)] out MonkeySyncObjectRegistration<TLink>? registeredSyncObject)
                => _registeredObjectsByName.TryGetValue(name, out registeredSyncObject);

            public static bool TryGetSyncObjectRegistration(Type type, [NotNullWhen(true)] out MonkeySyncObjectRegistration<TLink>? registeredSyncObject)
                => _registeredObjectByType.TryGetValue(type, out registeredSyncObject);

            public static bool UnregisterLinkedSyncObject(TLink linkObject, bool dispose)
            {
                if (!dispose)
                    return _syncObjectsByLinkObject.Remove(linkObject);

                if (!_syncObjectsByLinkObject.TryGetValue(linkObject, out var syncObject))
                    return false;

                (syncObject as IDisposable)?.Dispose();

                return true;
            }

            public static bool UnregisterLinkedSyncObject(ILinkedMonkeySyncObject<TLink> syncObject)
                => _syncObjectsByLinkObject.Remove(syncObject.LinkObject);

            public static bool UnregisterSyncObject(string name)
            {
                if (!TryGetSyncObjectRegistration(name, out var registeredObject))
                    return false;

                if (!TryGetSyncObjectRegistration(registeredObject.SyncObjectType, out var registeredObject2) || registeredObject.Name != registeredObject2.Name)
                    throw new InvalidOperationException($"No Sync Object type found using type [{registeredObject.SyncObjectType.CompactDescription()} based on the name [{name}], or name [{registeredObject2?.Name ?? "N/A"}] doesn't match!");

                _registeredObjectsByName.Remove(name);
                _registeredObjectByType.Remove(registeredObject.SyncObjectType);

                return true;
            }

            public static bool UnregisterSyncObject(Type syncObjectType)
            {
                if (!TryGetSyncObjectRegistration(syncObjectType, out var registeredObject))
                    return false;

                if (!TryGetSyncObjectRegistration(registeredObject.Name, out var registeredObject2) || registeredObject.SyncObjectType != registeredObject2.SyncObjectType)
                    throw new InvalidOperationException($"No Sync Object type found using name [{registeredObject.Name} based on the type [{syncObjectType.CompactDescription()}], or type [{registeredObject2?.SyncObjectType.CompactDescription() ?? "N/A"}] doesn't match!");

                _registeredObjectByType.Remove(syncObjectType);
                _registeredObjectsByName.Remove(registeredObject.Name);

                return true;
            }
        }
    }
}