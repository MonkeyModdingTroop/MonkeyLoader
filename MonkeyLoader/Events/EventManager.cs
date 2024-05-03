using HarmonyLib;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed class EventManager
    {
        private static readonly MethodInfo _registerAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterAsyncEventSource));
        private static readonly MethodInfo _registerCancelableAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableAsyncEventSource));
        private static readonly MethodInfo _registerCancelableSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableSyncEventSource));
        private static readonly MethodInfo _registerSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterSyncEventSource));
        private static readonly MethodInfo _unregisterAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterAsyncEventSource));
        private static readonly MethodInfo _unregisterCancelableAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableAsyncEventSource));
        private static readonly MethodInfo _unregisterCancelableSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableSyncEventSource));
        private static readonly MethodInfo _unregisterSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterSyncEventSource));

        private readonly AnyMap _eventDispatchers = new();
        private readonly MonkeyLoader _loader;
        internal Logger Logger { get; }

        internal EventManager(MonkeyLoader loader)
        {
            _loader = loader;
            Logger = new(loader.Logger, "EventManager");
        }

        internal bool RegisterAsyncEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent>).AddHandler(mod, asyncEventHandler);
        }

        internal bool RegisterCancelableAsyncEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent>).AddHandler(mod, cancelableAsyncEventHandler);
        }

        internal bool RegisterCancelableSyncEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : SyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent>).AddHandler(mod, cancelableEventHandler);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerAsyncEventSource, mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerCancelableAsyncEventSource, mod, cancelableEventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : SyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerCancelableSyncEventSource, mod, cancelableEventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerSyncEventSource, mod, eventSource);
        }

        internal bool RegisterSyncEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent>).AddHandler(mod, eventHandler);
        }

        internal bool UnregisterAsyncEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, asyncEventHandler);

            return false;
        }

        internal bool UnregisterCancelableAsyncEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableAsyncEventHandler);

            return false;
        }

        internal bool UnregisterCancelableSyncEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : SyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableEventHandler);

            return false;
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterAsyncEventSource, mod, eventSource);
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterCancelableAsyncEventSource, mod, cancelableEventSource);
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : SyncEvent, ICancelableEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterCancelableSyncEventSource, mod, cancelableEventSource);
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterSyncEventSource, mod, eventSource);
        }

        internal void UnregisterMod(Mod mod)
        {
            ValidateLoader(mod);

            foreach (var eventDispatcher in _eventDispatchers.GetCastableValues<IEventDispatcher>())
                eventDispatcher.UnregisterMod(mod);
        }

        internal bool UnregisterSyncEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, eventHandler);

            return false;
        }

        private AsyncEventDispatcher<TEvent> CreateAsyncDispatcher<TEvent>()
            where TEvent : AsyncEvent => new(this);

        private CancelableAsyncEventDispatcher<TEvent> CreateCancelableAsyncDispatcher<TEvent>()
            where TEvent : AsyncEvent, ICancelableEvent => new(this);

        private CancelableEventDispatcher<TEvent> CreateCancelableDispatcher<TEvent>()
            where TEvent : SyncEvent, ICancelableEvent => new(this);

        private EventDispatchers<TEvent> CreateDispatcher<TEvent>()
            where TEvent : SyncEvent => new(this);

        private bool InvokeMethodForAllDispatchableEvents(Type concreteEventType, MethodInfo method, params object[] parameters)
        {
            var done = false;

            foreach (var eventType in Event.GetDispatchableEventTypes(concreteEventType))
            {
                done |= (bool)method.MakeGenericMethod(eventType).Invoke(this, parameters);
            }

            return done;
        }

        private bool RegisterAsyncEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent>).AddSource(mod, eventSource);
        }

        private bool RegisterCancelableAsyncEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent>).AddSource(mod, cancelableEventSource);
        }

        private bool RegisterCancelableSyncEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : SyncEvent, ICancelableEvent
        {
            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent>).AddSource(mod, cancelableEventSource);
        }

        private bool RegisterSyncEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
        {
            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent>).AddSource(mod, eventSource);
        }

        private bool UnregisterAsyncEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        private bool UnregisterCancelableAsyncEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : AsyncEvent, ICancelableEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        private bool UnregisterCancelableSyncEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : SyncEvent, ICancelableEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        private bool UnregisterSyncEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
        {
            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        private void ValidateLoader(Mod mod)
        {
            if (mod.Loader != _loader)
                throw new InvalidOperationException("Can't (un)register event handler of mod from another loader!");
        }
    }
}