using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks the base for all synchronous events used by <see cref="ICancelableEventSource{TEvent}"/>s.
    /// </summary>
    public abstract class CancelableSyncEvent : SyncEvent, ICancelableEvent
    {
        /// <inheritdoc/>
        public bool Canceled { get; set; }

        protected CancelableSyncEvent()
        { }
    }

    /// <summary>
    /// Marks the base for all cancelable synchronous events used by <see cref="IEventSource{TEvent}"/>s.
    /// </summary>
    public abstract class SyncEvent : Event
    {
        protected SyncEvent()
        { }
    }
}