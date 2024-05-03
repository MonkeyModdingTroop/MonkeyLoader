using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks the base for all asynchronous events used by
    /// <see cref="IAsyncEventSource{TEvent}"/> and <see cref="ICancelableAsyncEventHandler{TEvent}"/>.
    /// </summary>
    public abstract class AsyncEvent : Event
    {
        protected AsyncEvent()
        { }
    }
}