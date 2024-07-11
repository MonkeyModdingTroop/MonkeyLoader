using System;
using System.Collections.Generic;
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
    public abstract class Event
    {
        private static readonly Type _asyncEventType = typeof(AsyncEvent);

        private static readonly HashSet<Type> _baseTypes;

        private static readonly Type _cancelableAsyncEventType = typeof(CancelableAsyncEvent);
        private static readonly Type _cancelableEventType = typeof(ICancelableEvent);
        private static readonly Type _cancelableSyncEventType = typeof(CancelableSyncEvent);
        private static readonly Type _eventType = typeof(Event);
        private static readonly Type _syncEventType = typeof(SyncEvent);

        static Event()
        {
            _baseTypes = new()
            {
                typeof(object), _eventType,
                _syncEventType, _cancelableSyncEventType,
                _asyncEventType, _cancelableAsyncEventType
            };
        }

        internal Event()
        { }

        /// <summary>
        /// Enumerates all <see cref="Type"/>s in the given <see cref="Event"/> <see cref="Type"/>'s
        /// hierarchy which events should be <see cref="DispatchableBaseEventAttribute">dispatched</see> for.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to dispatch.</param>
        /// <returns>The concrete <paramref name="eventType"/> and any <see cref="DispatchableBaseEventAttribute">dispatchable</see> base <see cref="Type"/>s.</returns>
        public static IEnumerable<Type> GetDispatchableEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || _baseTypes.Contains(eventType))
                yield break;

            yield return eventType;

            eventType = eventType.BaseType;

            while (!_baseTypes.Contains(eventType))
            {
                if (eventType.GetCustomAttribute<DispatchableBaseEventAttribute>() is not null)
                    yield return eventType;

                eventType = eventType.BaseType;
            }
        }

        /// <summary>
        /// Determines whether the given <see cref="Event"/> <see cref="Type"/> is an <see cref="AsyncEvent"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to check.</param>
        /// <returns><c>true</c> if it is an <see cref="AsyncEvent"/>; otherwise, <c>false</c>.</returns>
        public static bool IsAsyncEvent(Type eventType)
            => _asyncEventType.IsAssignableFrom(eventType);

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