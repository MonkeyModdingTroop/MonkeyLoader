using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Defines the non-generic interface for <see cref="MonkeySyncObject{TSyncObject,
    /// TSyncValues, TLink}">MonkeySync objects</see>.
    /// </summary>
    public interface IMonkeySyncObject : INotifyPropertyChanged
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
    /// TSyncValues, TLink}">MonkeySync objects</see>.
    /// </summary>
    /// <typeparam name="TLink">The type of the link object used by the sync object.</typeparam>
    public interface IMonkeySyncObject<out TLink> : IMonkeySyncObject
        where TLink : class
    {
        /// <inheritdoc cref="IMonkeySyncObject.LinkObject"/>
        public new TLink LinkObject { get; }
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
    public abstract class MonkeySyncObject<TSyncObject, TSyncValue, TLink> : IMonkeySyncObject<TLink>
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
        /// Establishes this sync object's link with the given object.
        /// </summary>
        /// <remarks>
        /// If the link fails or gets broken, a new instance has to be created.
        /// </remarks>
        /// <param name="linkObject">The link object to be used by this sync object.</param>
        /// <returns><c>true</c> if the established link is valid; otherwise, <c>false</c>.</returns>
        public bool LinkWith(TLink linkObject)
        {
            if (HasLinkObject)
                throw new InvalidOperationException("Can only assign a link object once!");

            LinkObject = linkObject;

            return EstablishLinkWith(linkObject);
        }

        /// <summary>
        /// Creates a link for the given sync value of the given name.
        /// </summary>
        /// <param name="propertyName">The name of the sync value to link.</param>
        /// <param name="syncValue">The sync value to link.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected abstract bool EstablishLinkFor(string propertyName, TSyncValue syncValue);

        /// <summary>
        /// Creates a link for the given sync method of the given name.
        /// </summary>
        /// <param name="methodName">The name of the sync method to link.</param>
        /// <param name="syncMethod">The sync method to link.</param>
        /// <returns><c>true</c> if the link was successfully created; otherwise, <c>false</c>.</returns>
        protected abstract bool EstablishLinkFor(string methodName, Action<TSyncObject> syncMethod);

        /// <remarks><para>
        /// <i>By default:</i> Calls <see cref="EstablishLinkFor(string, TSyncValue)">EstablishLinkFor</see>
        /// for every readable <typeparamref name="TSyncValue"/> instance property and
        /// <see cref="EstablishLinkFor(string, TSyncValue)">its overload</see> for every
        /// <see cref="MonkeySyncMethodAttribute">MonkeySync method</see> on <typeparamref name="TSyncObject"/>.<br/>
        /// The detected properties are stored in <see cref="propertyAccessorsByName">propertyAccessorsByName</see>,
        /// while the detected methods are stored in <see cref="methodsByName">methodsByName</see>.
        /// </para><para>
        /// This method is called by <see cref="LinkWith">LinkWith</see>
        /// after the <see cref="LinkObject">LinkObject</see> has been assigned.
        /// </para>
        /// </remarks>
        /// <inheritdoc cref="LinkWith"/>
        protected virtual bool EstablishLinkWith(TLink linkObject)
        {
            var success = true;

            foreach (var syncValueProperty in propertyAccessorsByName)
                success &= EstablishLinkFor(syncValueProperty.Key, syncValueProperty.Value((TSyncObject)this));

            foreach (var syncMethod in methodsByName)
                success &= EstablishLinkFor(syncMethod.Key, syncMethod.Value);

            return success;
        }

        /// <summary>
        /// Triggers the <see cref="PropertyChanged">PropertyChanged</see>
        /// event with the given <paramref name="propertyName"/>.
        /// </summary>
        /// <remarks>
        /// This is automatically called for any <see cref="MonkeySyncValue{T}"/> properties.
        /// </remarks>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName)
        {
            var eventData = new PropertyChangedEventArgs(propertyName);

            PropertyChanged?.Invoke(this, eventData);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}