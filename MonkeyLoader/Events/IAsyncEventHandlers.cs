using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for async event handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of cancelable async events handled.</typeparam>
    public interface IAsyncEventHandler<in TEvent> : IPrioritizable
        where TEvent : class
    {
        /// <summary>
        /// Handles the given async event based on its data.
        /// </summary>
        /// <param name="eventData">An object containing all the relevant information for the async event.</param>
        public Task Handle(TEvent eventData);
    }

    /// <summary>
    /// Defines the interface for async event handlers that support cancelation.
    /// </summary>
    /// <typeparam name="TEvent">The type of cancelable async events handled.</typeparam>
    public interface ICancelableAsyncEventHandler<in TEvent> : IPrioritizable
        where TEvent : class, ICancelableEvent
    {
        /// <summary>
        /// Gets whether this handler should be skipped for async events that have been
        /// canceled by a previous <see cref="ICancelableAsyncEventHandler{TEvent}">async event handler</see>.
        /// </summary>
        public bool SkipCanceled { get; }

        /// <summary>
        /// Handles the given cancelable async event based on its data.
        /// </summary>
        /// <remarks>
        /// When this method sets <c><paramref name="eventData"/>.<see cref="ICancelableEvent.Canceled">Canceled</see>
        /// = true</c>, the default action should be prevented from happening and further
        /// <see cref="ICancelableAsyncEventHandler{TEvent}">async event handlers</see> may be skipped.
        /// </remarks>
        /// <param name="eventData">An object containing all the relevant information for the async event.</param>
        public Task Handle(TEvent eventData);
    }
}