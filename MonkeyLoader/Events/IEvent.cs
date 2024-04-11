using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for cancelable events with a
    /// <see cref="IEvent{TTarget}.Target">target object</see> that's the focus of the event.
    /// </summary>
    /// <inheritdoc/>
    public interface ICancelableEvent<out TTarget> : IEvent<TTarget>
    {
        /// <summary>
        /// Gets or sets whether this event has been canceled by
        /// a previous <see cref="ICancelableEventHandler{TEvent, TTarget}">event handler</see>.
        /// </summary>
        /// <remarks>
        /// When this property is <c>true</c>, the default action should be prevented from happening.
        /// </remarks>
        public bool Canceled { get; set; }
    }

    /// <summary>
    /// Defines the interface for events with a
    /// <see cref="Target">target object</see> that's the focus of the event.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object that is the focus of the event.</typeparam>
    public interface IEvent<out TTarget>
    {
        /// <summary>
        /// Gets the target object that is the focus of the event.
        /// </summary>
        public TTarget Target { get; }
    }
}