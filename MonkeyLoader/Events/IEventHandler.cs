using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for event handlers that support cancelation.
    /// </summary>
    /// <typeparam name="TEvent">The type of cancelable events handled.</typeparam>
    /// <typeparam name="TTarget">The type of the target objects that are the focus of the events.</typeparam>
    public interface ICancelableEventHandler<in TEvent, out TTarget> : IPrioritizable
        where TEvent : ICancelableEvent<TTarget>
    {
        /// <summary>
        /// Gets whether this handler should be skipped for events that have been
        /// canceled by a previous <see cref="ICancelableEventHandler{TEvent, TTarget}">event handler</see>.
        /// </summary>
        public bool SkipCanceled { get; }

        /// <summary>
        /// Handles the given cancelable event.
        /// </summary>
        /// <remarks>
        /// When this method sets <c><paramref name="eventArgs"/>.<see cref="ICancelableEvent{TTarget}.Canceled">Canceled</see>
        /// = true</c>, the default action should be prevented from happening and further
        /// <see cref="ICancelableEventHandler{TEvent, TTarget}">event handlers</see> may be skipped.
        /// </remarks>
        /// <param name="eventArgs">An object containing all the relevant information for the event.</param>
        public void Handle(TEvent eventArgs);
    }

    /// <summary>
    /// Defines the interface for event handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of cancelable events handled.</typeparam>
    /// <typeparam name="TTarget">The type of the target objects that are the focus of the events.</typeparam>
    public interface IEventHandler<in TEvent, out TTarget> : IPrioritizable
        where TEvent : IEvent<TTarget>
    {
        /// <summary>
        /// Handles the given event.
        /// </summary>
        /// <param name="eventArgs">An object containing all the relevant information for the event.</param>
        public void Handle(TEvent eventArgs);
    }
}