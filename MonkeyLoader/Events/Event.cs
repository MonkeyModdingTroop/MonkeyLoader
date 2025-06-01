using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks the base for all event data classes.<br/>
    /// Also contains static helper methods.
    /// </summary>
    public abstract partial class Event
    {
        private static readonly Type _asyncEventType = typeof(AsyncEvent);

        private static readonly HashSet<Type> _baseTypes;

        private static readonly Type _cancelableAsyncEventType = typeof(CancelableAsyncEvent);
        private static readonly Type _cancelableEventType = typeof(ICancelableEvent);
        private static readonly Type _cancelableSyncEventType = typeof(CancelableSyncEvent);
        private static readonly Type _eventType = typeof(Event);
        private static readonly Type _syncEventType = typeof(SyncEvent);

        /// <summary>
        /// Gets whether this event is an <see cref="AsyncEvent"/>.
        /// </summary>
        public abstract bool IsAsync { get; }

        /// <summary>
        /// Gets whether this event can be <see cref="ICancelableEvent.Canceled">canceled</see>.
        /// </summary>
        /// <value><see langword="true"/> if this event implements <see cref="ICancelableEvent"/>; otherwise, <see langword="false"/>.</value>
        public virtual bool IsCancelable => this is ICancelableEvent;

        static Event()
        {
            _baseTypes =
            [
                typeof(object), _eventType,
                _syncEventType, _cancelableSyncEventType,
                _asyncEventType, _cancelableAsyncEventType
            ];
        }

        // Make sure this stays private protected
        internal Event()
        { }

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is an <see cref="AsyncEvent"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is an <see cref="AsyncEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool IsAsyncEvent(Type eventType)
            => _asyncEventType.IsAssignableFrom(eventType);

        /// <summary>
        /// Determines whether the given <see cref="Type"/> is a base <see cref="Event"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is a base <see cref="Event"/>; otherwise, <c>false</c>.</returns>
        public static bool IsBaseEvent(Type type)
            => _baseTypes.Contains(type);

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is cancelable.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is cancelable; otherwise, <c>false</c>.</returns>
        public static bool IsCancelable(Type eventType)
            => _cancelableEventType.IsAssignableFrom(eventType);

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is a <see cref="CancelableAsyncEvent"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is a <see cref="CancelableAsyncEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool IsCancelableAsyncEvent(Type eventType)
            => _cancelableAsyncEventType.IsAssignableFrom(eventType);

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is a <see cref="CancelableSyncEvent"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is a <see cref="CancelableSyncEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool IsCancelableSyncEvent(Type eventType)
            => _cancelableSyncEventType.IsAssignableFrom(eventType);

        /// <summary>
        /// Determines whether the given <see cref="Type"/> is an <see cref="Event"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is an <see cref="Event"/>; otherwise, <c>false</c>.</returns>
        public static bool IsEvent(Type type)
            => _eventType.IsAssignableFrom(type);

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is a <see cref="SyncEvent"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is a <see cref="SyncEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool IsSyncEvent(Type eventType)
            => _syncEventType.IsAssignableFrom(eventType);
    }
}