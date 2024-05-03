using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for synchronous event handlers that support cancelation.
    /// </summary>
    /// <typeparam name="TEvent">The type of cancelable events handled.</typeparam>
    public interface ICancelableEventHandler<in TEvent> : IPrioritizable
        where TEvent : CancelableSyncEvent
    {
        /// <summary>
        /// Gets whether this handler should be skipped for events
        /// that have been canceled by a previous event handler.
        /// </summary>
        public bool SkipCanceled { get; }

        /// <summary>
        /// Handles the given cancelable synchronous event based on its data.
        /// </summary>
        /// <remarks>
        /// When this method sets <c><paramref name="eventData"/>.<see cref="CancelableSyncEvent.Canceled">Canceled</see>
        /// = true</c>, the default action should be prevented from happening and further event handlers may be skipped.
        /// </remarks>
        /// <param name="eventData">An object containing all the relevant information for the cancelable event.</param>
        public void Handle(TEvent eventData);
    }

    /// <summary>
    /// Defines the interface for synchronous event handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of synchronous events handled.</typeparam>
    public interface IEventHandler<in TEvent> : IPrioritizable
        where TEvent : SyncEvent
    {
        /// <summary>
        /// Handles the given synchronous event based on its data.
        /// </summary>
        /// <param name="eventData">An object containing all the relevant information for the synchronous event.</param>
        public void Handle(TEvent eventData);
    }
}