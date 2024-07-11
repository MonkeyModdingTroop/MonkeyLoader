using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks the base for all asynchronous events used by <see cref="IAsyncEventSource{TEvent}"/>s.
    /// </summary>
    public abstract class AsyncEvent : Event
    {
        /// <summary>
        /// Initializes this asynchronous event.
        /// </summary>
        protected AsyncEvent()
        { }
    }

    /// <summary>
    /// Marks the base for all cancelable asynchronous events used by <see cref="ICancelableAsyncEventHandler{TEvent}"/>s.
    /// </summary>
    public abstract class CancelableAsyncEvent : AsyncEvent, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        /// <summary>
        /// Initializes this cancelable asynchronous event.
        /// </summary>
        protected CancelableAsyncEvent()
        { }
    }
}