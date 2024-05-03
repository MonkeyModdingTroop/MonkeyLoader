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
        private static readonly Type _cancelableType = typeof(ICancelableEvent);
        private static readonly Type _eventType = typeof(Event);
        private static readonly Type _objectType = typeof(object);
        private static readonly Type _syncEventType = typeof(SyncEvent);

        internal Event()
        { }

        public static IEnumerable<Type> GetDispatchableEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || eventType.BaseType == _eventType || eventType == _eventType || eventType == _objectType)
                yield break;

            yield return eventType;

            var isCancelable = IsCancelable(eventType);
            eventType = eventType.BaseType;

            while (eventType.BaseType != _eventType)
            {
                if (eventType.GetCustomAttribute<DispatchableBaseEventAttribute>() is not null && IsCancelable(eventType) == isCancelable)
                    yield return eventType;

                eventType = eventType.BaseType;
            }
        }

        public static bool IsAsyncEvent(Type eventType)
            => _asyncEventType.IsAssignableFrom(eventType);

        public static bool IsCancelable(Type eventType)
            => _cancelableType.IsAssignableFrom(eventType);

        public static bool IsEvent(Type eventType)
            => _eventType.IsAssignableFrom(eventType);

        public static bool IsSyncEvent(Type eventType)
            => _syncEventType.IsAssignableFrom(eventType);
    }
}