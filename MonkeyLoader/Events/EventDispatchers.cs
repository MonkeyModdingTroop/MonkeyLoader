using HarmonyLib;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Concrete dispatcher for <see cref="CancelableSyncEvent"/>s.
    /// </summary>
    /// <inheritdoc cref="EventDispatcher{TEvent}"/>
    internal sealed class CancelableEventDispatcher<TEvent>
            : EventDispatcherBase<ICancelableEventSource<TEvent>, ICancelableEventHandler<TEvent>>
        where TEvent : CancelableSyncEvent
    {
        /// <summary>
        /// Creates a new dispatcher for the given <paramref name="manager"/>.
        /// </summary>
        /// <param name="manager">The manager that this dispatcher belongs to.</param>
        public CancelableEventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(CancelableEventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        /// <inheritdoc cref="EventDispatcher{TEvent}.AddSource"/>
        public bool AddSource<TDerivedEvent>(Mod mod, ICancelableEventSource<TDerivedEvent> source)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), source))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            source.Dispatching += eventDispatcher;

            return true;
        }

        /// <inheritdoc cref="EventDispatcher{TEvent}.RemoveSource"/>
        public bool RemoveSource<TDerivedEvent>(Mod mod, ICancelableEventSource<TDerivedEvent> source)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), source))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            source.Dispatching -= eventDispatcher;

            return true;
        }

        private void DispatchEvents(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                if (eventArgs.Canceled && handler.SkipCanceled)
                {
                    Logger.Trace(() => $"Skipping event handler [{handler.GetType().CompactDescription()}] for canceled event [{eventArgs}]!");
                    continue;
                }

                try
                {
                    handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.LogFormat($"Event handler [{handler.GetType().CompactDescription()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }

        private CancelableEventDispatching<TDerivedEvent> MakeEventDispatcher<TDerivedEvent>()
            where TDerivedEvent : TEvent => new(DispatchEvents);
    }

    /// <summary>
    /// Concrete dispatcher for <see cref="SyncEvent"/>s.
    /// </summary>
    /// <typeparam name="TEvent">The type of the dispatched events.</typeparam>
    internal sealed class EventDispatcher<TEvent>
            : EventDispatcherBase<IEventSource<TEvent>, IEventHandler<TEvent>>
        where TEvent : SyncEvent
    {
        /// <summary>
        /// Creates a new dispatcher for the given <paramref name="manager"/>.
        /// </summary>
        /// <param name="manager">The manager that this dispatcher belongs to.</param>
        public EventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(EventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        /// <summary>
        /// Adds the <typeparamref name="TDerivedEvent"/> <paramref name="source"/>
        /// from the given <paramref name="mod"/> to this dispatcher.
        /// </summary>
        /// <typeparam name="TDerivedEvent">The concrete type of the events created by the source.</typeparam>
        /// <inheritdoc cref="EventDispatcherBase{TSource, THandler}.AddSource"/>
        public bool AddSource<TDerivedEvent>(Mod mod, IEventSource<TDerivedEvent> source)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), source))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            source.Dispatching += eventDispatcher;

            return true;
        }

        /// <summary>
        /// Removes the <typeparamref name="TDerivedEvent"/> <paramref name="source"/>
        /// from the given <paramref name="mod"/> from this dispatcher.
        /// </summary>
        /// <typeparam name="TDerivedEvent">The concrete type of the events created by the source.</typeparam>
        /// <inheritdoc cref="EventDispatcherBase{TSource, THandler}.RemoveSource"/>
        public bool RemoveSource<TDerivedEvent>(Mod mod, IEventSource<TDerivedEvent> source)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), source))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            source.Dispatching -= eventDispatcher;

            return true;
        }

        private void DispatchEvents(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.LogFormat($"Event handler [{handler.GetType().CompactDescription()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }

        private EventDispatching<TDerivedEvent> MakeEventDispatcher<TDerivedEvent>()
            where TDerivedEvent : TEvent => new(DispatchEvents);
    }
}