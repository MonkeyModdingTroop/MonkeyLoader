using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace MonkeyLoader.Events
{
    public abstract partial class Event
    {
        private static readonly Dictionary<Type, ImmutableArray<Type>> _subscribableBaseEventTypesByConcreteType = [];

        /// <summary>
        /// Enumerates all <see cref="SubscribableBaseEventAttribute">subscribable</see>
        /// base <see cref="Type"/>s of the given <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> being analyzed.</param>
        /// <returns>Any <see cref="SubscribableBaseEventAttribute">subscribable</see> base <see cref="Type"/>s of the <paramref name="eventType"/>.</returns>
        public static IEnumerable<Type> GetSubscribableBaseEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || IsBaseEvent(eventType))
                return ImmutableArray<Type>.Empty;

            if (!_subscribableBaseEventTypesByConcreteType.TryGetValue(eventType, out var baseEvents))
            {
                baseEvents = GetSubscribableBaseEventTypesInternal(eventType.BaseType!);
                _subscribableBaseEventTypesByConcreteType.Add(eventType, baseEvents);
            }

            return baseEvents;
        }

        /// <summary>
        /// Enumerates all <see cref="Type"/>s in the given <see cref="Event"/> <see cref="Type"/>'s
        /// hierarchy which handlers for the <paramref name="eventType"/> should be subscribed to.
        /// </summary>
        /// <param name="eventType">The concrete <see cref="Event"/> <see cref="Type"/> being handled.</param>
        /// <returns>The <paramref name="eventType"/> and any <see cref="SubscribableBaseEventAttribute">subscribable</see> base <see cref="Type"/>s of it.</returns>
        public static IEnumerable<Type> GetSubscribableEventTypes(Type eventType)
        {
            if (!IsEvent(eventType) || IsBaseEvent(eventType))
                yield break;

            yield return eventType;

            foreach (var baseEventType in GetSubscribableBaseEventTypes(eventType))
                yield return baseEventType;
        }

        private static ImmutableArray<Type> GetSubscribableBaseEventTypesInternal(Type eventType)
        {
            if (IsBaseEvent(eventType))
                return ImmutableArray<Type>.Empty;

            if (!_subscribableBaseEventTypesByConcreteType.TryGetValue(eventType, out var subscribableBaseEventTypes))
                subscribableBaseEventTypes = GetSubscribableBaseEventTypesInternal(eventType.BaseType!);

            if (eventType.GetCustomAttribute<SubscribableBaseEventAttribute>() is not null)
                subscribableBaseEventTypes = subscribableBaseEventTypes.Insert(0, eventType);

            return subscribableBaseEventTypes;
        }
    }

    /// <summary>
    /// Marks an <see cref="Event"/>-derived class as the base class of
    /// the more derived instances dispatched by sources.<br/>
    /// This causes handlers expecting a more derived event type to be subscribed to
    /// sources for the marked class too; only being called when
    /// the concrete dispatched event is compatible with what they expect.
    /// </summary>
    /// <remarks>
    /// This is primarily intended for sources dispatching different event classes
    /// that involve a generic parameter, to avoid having to exhaustively list them.
    /// </remarks>
    public sealed class SubscribableBaseEventAttribute : EventAttribute
    { }
}