using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// A simple data class that allows the <see cref="MonkeySyncRegistry"/> to track the known
    /// <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see> types.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    public sealed class MonkeySyncObjectRegistration<TLink>
        where TLink : class
    {
        private readonly SyncObjectFactory<TLink> _createSyncObject;

        /// <summary>
        /// Gets the <typeparamref name="TLink"/>-unique name for the registered
        /// <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see> type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the registered
        /// <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see>.
        /// </summary>
        public Type SyncObjectType { get; }

        /// <summary>
        /// Creates a new instance of this data class with the given details for a
        /// <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see> type.
        /// </summary>
        /// <param name="name">The <typeparamref name="TLink"/>-unique name for the sync object type.</param>
        /// <param name="syncObjectType">The type of the sync object.</param>
        /// <param name="createSyncObject">A factory method that creates new instances of this sync object type.</param>
        public MonkeySyncObjectRegistration(string name, Type syncObjectType, SyncObjectFactory<TLink> createSyncObject)
        {
            Name = name;
            SyncObjectType = syncObjectType;
            _createSyncObject = createSyncObject;
        }

        /// <summary>
        /// Creates a new instance of the registered
        /// <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync object</see>.
        /// </summary>
        /// <returns>The created but not yet linked sync object.</returns>
        public IUnlinkedMonkeySyncObject<TLink> CreateSyncObject()
            => _createSyncObject();
    }
}