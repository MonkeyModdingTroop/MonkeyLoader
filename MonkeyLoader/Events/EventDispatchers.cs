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
    internal sealed class CancelableEventDispatcher<TEvent>
            : EventDispatcherBase<ICancelableEventSource<TEvent>, ICancelableEventHandler<TEvent>>
        where TEvent : CancelableSyncEvent
    {
        public CancelableEventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(CancelableEventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        public bool AddSource<TDerivedEvent>(Mod mod, ICancelableEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching += eventDispatcher;

            return true;
        }

        public bool RemoveSource<TDerivedEvent>(Mod mod, ICancelableEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching -= eventDispatcher;

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

    internal sealed class EventDispatcher<TEvent>
            : EventDispatcherBase<IEventSource<TEvent>, IEventHandler<TEvent>>
        where TEvent : SyncEvent
    {
        public EventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(EventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        public bool AddSource<TDerivedEvent>(Mod mod, IEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching += eventDispatcher;

            return true;
        }

        public bool RemoveSource<TDerivedEvent>(Mod mod, IEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching -= eventDispatcher;

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