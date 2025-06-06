﻿using System;
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

        /// <value>Always <see langword="true"/>.</value>
        /// <inheritdoc/>
        public override sealed bool IsCancelable => true;

        /// <summary>
        /// Initializes this cancelable synchronous event.
        /// </summary>
        protected CancelableSyncEvent()
        { }
    }

    /// <summary>
    /// Marks the base for all synchronous events used by <see cref="IEventSource{TEvent}"/>s.
    /// </summary>
    public abstract class SyncEvent : Event
    {
        /// <value>Always <see langword="false"/>.</value>
        /// <inheritdoc/>
        public override sealed bool IsAsync => false;

        /// <summary>
        /// Initializes this synchronous event.
        /// </summary>
        protected SyncEvent()
        { }
    }
}