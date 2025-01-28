using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Handles globally registering <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValue, TLink}">MonkeySync objects</see> for particular link types,
    /// so that the link implementations can create them.
    /// </summary>
    public static class MonkeySyncRegistry
    {
        /// <summary>
        /// Gets the <see cref="RegisteredSyncObject{TLink}"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The name of the registered sync object.</param>
        /// <returns>The found <see cref="RegisteredSyncObject{TLink}"/>.</returns>
        /// <exception cref="KeyNotFoundException">When no sync object with the given <paramref name="name"/> has been registered.</exception>
        public static RegisteredSyncObject<TLink> GetRegisteredSyncObject<TLink>(string name)
                where TLink : class
            => Library<TLink>.GetRegisteredSyncObject(name);

        /// <summary>
        /// Gets the <see cref="RegisteredSyncObject{TLink}"/> for the given <paramref name="syncObjectType"/>.
        /// </summary>
        /// <param name="syncObjectType">The type of registered sync object.</param>
        /// <inheritdoc cref="GetRegisteredSyncObject{TLink}(string)"/>
        public static RegisteredSyncObject<TLink> GetRegisteredSyncObject<TLink>(Type syncObjectType)
                where TLink : class
            => Library<TLink>.GetRegisteredSyncObject(syncObjectType);

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
        public static RegisteredSyncObject<TLink> RegisterSyncObject<TLink>(string name, Type syncObjectType, SyncObjectFactory<TLink> createSyncObject)
                where TLink : class
            => Library<TLink>.RegisterSyncObject(name, syncObjectType, createSyncObject);

        /// <summary>
        /// Tries to get the <see cref="RegisteredSyncObject{TLink}"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
        /// <param name="name">The name of the registered sync object.</param>
        /// <param name="registeredSyncObject">The found <see cref="RegisteredSyncObject{TLink}"/>; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the registered sync object type was found; otherwise, <c>false</c>.</returns>
        public static bool TryGetRegisteredSyncObject<TLink>(string name, [NotNullWhen(true)] out RegisteredSyncObject<TLink>? registeredSyncObject)
                where TLink : class
            => Library<TLink>.TryGetRegisteredSyncObject(name, out registeredSyncObject);

        /// <summary>
        /// Tries to get the <see cref="RegisteredSyncObject{TLink}"/> for the given <paramref name="syncObjectType"/>.
        /// </summary>
        /// <param name="syncObjectType"></param>
        /// <param name="registeredSyncObject">The found <see cref="RegisteredSyncObject{TLink}"/>; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the registered sync object type was found; otherwise, <c>false</c>.</returns>
        public static bool TryGetRegisteredSyncObject<TLink>(Type syncObjectType, [NotNullWhen(true)] out RegisteredSyncObject<TLink>? registeredSyncObject)
                where TLink : class
            => Library<TLink>.TryGetRegisteredSyncObject(syncObjectType, out registeredSyncObject);

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
        /// but none with the associated <see cref="Type"/> - or there's a <see cref="RegisteredSyncObject{TLink}.Name">Name</see> mismatch.
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
        /// but none with the associated Name - or there's a <see cref="RegisteredSyncObject{TLink}.SyncObjectType">Type</see> mismatch.
        /// </exception>
        public static bool UnregisterSyncObject<TLink>(Type syncObjectType)
                where TLink : class
            => Library<TLink>.UnregisterSyncObject(syncObjectType);

        private static class Library<TLink>
            where TLink : class
        {
            private static readonly Dictionary<Type, RegisteredSyncObject<TLink>> _registeredObjectByType = [];
            private static readonly Dictionary<string, RegisteredSyncObject<TLink>> _registeredObjectsByName = new(StringComparer.Ordinal);

            public static RegisteredSyncObject<TLink> GetRegisteredSyncObject(string name)
                => _registeredObjectsByName[name];

            public static RegisteredSyncObject<TLink> GetRegisteredSyncObject(Type type)
                => _registeredObjectByType[type];

            public static RegisteredSyncObject<TLink> RegisterSyncObject(string name, Type syncObjectType, SyncObjectFactory<TLink> createSyncObject)
            {
                if (_registeredObjectsByName.ContainsKey(name) || _registeredObjectByType.ContainsKey(syncObjectType))
                    throw new ArgumentException($"Sync Object type [{syncObjectType.CompactDescription()}] with name [{name}] has already been registered!");

                var registeredObject = new RegisteredSyncObject<TLink>(name, syncObjectType, createSyncObject);

                _registeredObjectsByName.Add(name, registeredObject);
                _registeredObjectByType.Add(syncObjectType, registeredObject);

                return registeredObject;
            }

            public static bool TryGetRegisteredSyncObject(string name, [NotNullWhen(true)] out RegisteredSyncObject<TLink>? registeredSyncObject)
                => _registeredObjectsByName.TryGetValue(name, out registeredSyncObject);

            public static bool TryGetRegisteredSyncObject(Type type, [NotNullWhen(true)] out RegisteredSyncObject<TLink>? registeredSyncObject)
                => _registeredObjectByType.TryGetValue(type, out registeredSyncObject);

            internal static bool UnregisterSyncObject(string name)
            {
                if (!TryGetRegisteredSyncObject(name, out var registeredObject))
                    return false;

                if (!TryGetRegisteredSyncObject(registeredObject.SyncObjectType, out var registeredObject2) || registeredObject.Name != registeredObject2.Name)
                    throw new InvalidOperationException($"No Sync Object type found using type [{registeredObject.SyncObjectType.CompactDescription()} based on the name [{name}], or name [{registeredObject2?.Name ?? "N/A"}] doesn't match!");

                _registeredObjectsByName.Remove(name);
                _registeredObjectByType.Remove(registeredObject.SyncObjectType);

                return true;
            }

            internal static bool UnregisterSyncObject(Type syncObjectType)
            {
                if (!TryGetRegisteredSyncObject(syncObjectType, out var registeredObject))
                    return false;

                if (!TryGetRegisteredSyncObject(registeredObject.Name, out var registeredObject2) || registeredObject.SyncObjectType != registeredObject2.SyncObjectType)
                    throw new InvalidOperationException($"No Sync Object type found using name [{registeredObject.Name} based on the type [{syncObjectType.CompactDescription()}], or type [{registeredObject2?.SyncObjectType.CompactDescription() ?? "N/A"}] doesn't match!");

                _registeredObjectByType.Remove(syncObjectType);
                _registeredObjectsByName.Remove(registeredObject.Name);

                return true;
            }
        }
    }
}