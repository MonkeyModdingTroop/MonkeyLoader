using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Defines the non-generic interface for <see cref="MonkeySyncObject{TLink}">MonkeySync objects</see>.
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
        [MaybeNull]
        public object LinkObject { get; }
    }

    /// <summary>
    /// Defines the generic interface for <see cref="MonkeySyncObject{TLink}">MonkeySync objects</see>.
    /// </summary>
    public interface IMonkeySyncObject<out TLink> : IMonkeySyncObject
        where TLink : class
    {
        /// <inheritdoc cref="IMonkeySyncObject.LinkObject"/>
        [MaybeNull]
        public new TLink LinkObject { get; }
    }

    public abstract class MonkeySyncObject<TLink> : IMonkeySyncObject<TLink>
        where TLink : class
    {
        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(LinkObject))]
        public bool HasLinkObject => LinkObject is not null;

        /// <inheritdoc/>
        public abstract bool IsLinkValid { get; }

        /// <inheritdoc/>
        public TLink LinkObject { get; }

        object IMonkeySyncObject.LinkObject => LinkObject;

        /// <summary>
        /// Creates a new instance of this sync object with the given link object.
        /// </summary>
        /// <param name="linkObject">The link object used by this sync object.</param>
        protected MonkeySyncObject(TLink linkObject)
        {
            LinkObject = linkObject;
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