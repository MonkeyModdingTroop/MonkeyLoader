using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks an <see cref="Event"/>-derived class as to be dispatched,
    /// even if it's only a base class of the concrete event coming from the source.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DispatchableBaseEventAttribute : MonkeyLoaderAttribute
    { }

    public abstract partial class Event
    {
        private static readonly Dictionary<Type, ImmutableArray<Type>> _dispatchableEventTypesByConcreteType = [];

        /// <summary>
        /// Enumerates all <see cref="Type"/>s in the given <see cref="Event"/> <see cref="Type"/>'s
        /// hierarchy which events should be <see cref="DispatchableBaseEventAttribute">dispatched</see> for.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> to dispatch.</param>
        /// <returns>The concrete <paramref name="eventType"/> and any <see cref="DispatchableBaseEventAttribute">dispatchable</see> base <see cref="Type"/>s.</returns>
        public static IEnumerable<Type> GetDispatchableEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || IsBaseEvent(eventType))
                return ImmutableArray<Type>.Empty;

            if (!_dispatchableEventTypesByConcreteType.TryGetValue(eventType, out var dispatchableEventTypes))
            {
                dispatchableEventTypes = GetDispatchableEventTypesInternal(eventType).ToImmutableArray();
                _dispatchableEventTypesByConcreteType.Add(eventType, dispatchableEventTypes);
            }

            return dispatchableEventTypes;
        }

        private static IEnumerable<Type> GetDispatchableEventTypesInternal(Type eventType)
        {
            yield return eventType;

            eventType = eventType.BaseType;

            while (!IsBaseEvent(eventType))
            {
                if (eventType.GetCustomAttribute<DispatchableBaseEventAttribute>() is not null)
                    yield return eventType;

                eventType = eventType.BaseType;
            }
        }
    }
}