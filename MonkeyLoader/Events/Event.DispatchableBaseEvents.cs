using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks an <see cref="Event"/>-derived class as a useful base class of
    /// the more derived instances dispatched by sources.<br/>
    /// This causes sources dispatching derived event types to
    /// dispatch events for the marked class too.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for events where multiple sources may dispatch more
    /// concrete versions, but where the marked base event can be useful by itself.
    /// </remarks>
    public sealed class DispatchableBaseEventAttribute : EventAttribute
    { }

    public abstract partial class Event
    {
        private static readonly Dictionary<Type, ImmutableArray<Type>> _dispatchableBaseEventTypesByConcreteType = [];

        /// <summary>
        /// Enumerates all <see cref="DispatchableBaseEventAttribute">dispatchable</see>
        /// base <see cref="Type"/>s of the given <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> being analyzed.</param>
        /// <returns>Any <see cref="DispatchableBaseEventAttribute">dispatchable</see> base <see cref="Type"/>s of the <paramref name="eventType"/>.</returns>
        public static IEnumerable<Type> GetDispatchableBaseEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || IsBaseEvent(eventType))
                return ImmutableArray<Type>.Empty;

            if (!_dispatchableBaseEventTypesByConcreteType.TryGetValue(eventType, out var baseEvents))
            {
                baseEvents = GetDispatchableEventTypesInternal(eventType.BaseType!);
                _dispatchableBaseEventTypesByConcreteType.Add(eventType, baseEvents);
            }

            return baseEvents;
        }

        /// <summary>
        /// Enumerates all <see cref="Type"/>s in the given <see cref="Event"/> <see cref="Type"/>'s
        /// hierarchy which sources for the <paramref name="eventType"/> should dispatch for.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> being analyzed.</param>
        /// <returns>The <paramref name="eventType"/> and any <see cref="DispatchableBaseEventAttribute">dispatchable</see> base <see cref="Type"/>s of it.</returns>
        public static IEnumerable<Type> GetDispatchableEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || IsBaseEvent(eventType))
                yield break;

            yield return eventType;

            foreach (var baseEventType in GetDispatchableBaseEventTypes(eventType))
                yield return baseEventType;
        }

        private static ImmutableArray<Type> GetDispatchableEventTypesInternal(Type eventType)
        {
            if (IsBaseEvent(eventType))
                return ImmutableArray<Type>.Empty;

            if (!_dispatchableBaseEventTypesByConcreteType.TryGetValue(eventType, out var dispatchableBaseEventTypes))
                dispatchableBaseEventTypes = GetDispatchableEventTypesInternal(eventType.BaseType!);

            if (eventType.GetCustomAttribute<DispatchableBaseEventAttribute>() is not null)
                dispatchableBaseEventTypes = dispatchableBaseEventTypes.Insert(0, eventType);

            return dispatchableBaseEventTypes;
        }
    }
}