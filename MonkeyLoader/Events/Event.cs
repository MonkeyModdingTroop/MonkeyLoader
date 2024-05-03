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

        public static bool IsAsyncEvent(Type eventType)
            => _asyncEventType.IsAssignableFrom(eventType);

        public static bool IsCancelable(Type eventType)
            => _cancelableEventType.IsAssignableFrom(eventType);

        public static bool IsCancelableAsyncEvent(Type eventType)
            => _cancelableAsyncEventType.IsAssignableFrom(eventType);

        public static bool IsCancelableSyncEvent(Type eventType)
            => _cancelableSyncEventType.IsAssignableFrom(eventType);

        public static bool IsEvent(Type eventType)
            => _eventType.IsAssignableFrom(eventType);

        public static bool IsSyncEvent(Type eventType)
            => _syncEventType.IsAssignableFrom(eventType);
    }
}