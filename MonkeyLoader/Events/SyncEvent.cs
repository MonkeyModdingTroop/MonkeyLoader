using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks the base for all synchronous events used by
    /// <see cref="IEventSource{TEvent}"/> and <see cref="ICancelableEventHandler{TEvent}"/>.
    /// </summary>
    public abstract class SyncEvent : Event
    {
        protected SyncEvent()
        { }
    }
}