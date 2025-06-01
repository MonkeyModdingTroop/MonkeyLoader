using HarmonyLib;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed partial class EventManager
    {
        private static readonly MethodInfo _registerAsyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(RegisterAsyncEventHandler));
        private static readonly MethodInfo _registerCancelableAsyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableAsyncEventHandler));
        private static readonly MethodInfo _registerCancelableSyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableSyncEventHandler));
        private static readonly MethodInfo _registerSyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(RegisterSyncEventHandler));

        private static readonly MethodInfo _unregisterAsyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(UnregisterAsyncEventHandler));
        private static readonly MethodInfo _unregisterCancelableAsyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableAsyncEventHandler));
        private static readonly MethodInfo _unregisterCancelableSyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableSyncEventHandler));
        private static readonly MethodInfo _unregisterSyncEventHandler = AccessTools.Method(typeof(EventManager), nameof(UnregisterSyncEventHandler));

        internal bool RegisterEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _registerAsyncEventHandler, mod, asyncEventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : CancelableAsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _registerCancelableAsyncEventHandler, mod, cancelableAsyncEventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : CancelableSyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _registerCancelableSyncEventHandler, mod, cancelableEventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _registerSyncEventHandler, mod, eventHandler);
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _unregisterAsyncEventHandler, mod, asyncEventHandler);
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : CancelableAsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _unregisterCancelableAsyncEventHandler, mod, cancelableAsyncEventHandler);
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : CancelableSyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _unregisterCancelableSyncEventHandler, mod, cancelableEventHandler);
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllSubscribableEvents(typeof(TEvent), _unregisterSyncEventHandler, mod, eventHandler);
        }

        private bool RegisterAsyncEventHandler<TBaseEvent, TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TBaseEvent : AsyncEvent
            where TEvent : TBaseEvent
            => _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TBaseEvent>).AddHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(asyncEventHandler));

        private bool RegisterCancelableAsyncEventHandler<TBaseEvent, TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TBaseEvent : CancelableAsyncEvent
            where TEvent : TBaseEvent
            => _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TBaseEvent>).AddHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(cancelableAsyncEventHandler));

        private bool RegisterCancelableSyncEventHandler<TBaseEvent, TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TBaseEvent : CancelableSyncEvent
            where TEvent : TBaseEvent
            => _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TBaseEvent>).AddHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(cancelableEventHandler));

        private bool RegisterSyncEventHandler<TBaseEvent, TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TBaseEvent : SyncEvent
            where TEvent : TBaseEvent
            => _eventDispatchers.GetOrCreateValue(CreateDispatcher<TBaseEvent>).AddHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(eventHandler));

        private bool UnregisterAsyncEventHandler<TBaseEvent, TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TBaseEvent : AsyncEvent
            where TEvent : TBaseEvent
        {
            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var asyncEventDispatcher))
                return asyncEventDispatcher!.RemoveHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(asyncEventHandler));

            return false;
        }

        private bool UnregisterCancelableAsyncEventHandler<TBaseEvent, TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TBaseEvent : CancelableAsyncEvent
            where TEvent : TBaseEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableAsyncEventDispatcher))
                return cancelableAsyncEventDispatcher!.RemoveHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(cancelableAsyncEventHandler));

            return false;
        }

        private bool UnregisterCancelableSyncEventHandler<TBaseEvent, TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TBaseEvent : CancelableSyncEvent
            where TEvent : TBaseEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(cancelableEventHandler));

            return false;
        }

        private bool UnregisterSyncEventHandler<TBaseEvent, TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TBaseEvent : SyncEvent
            where TEvent : TBaseEvent
        {
            if (_eventDispatchers.TryGetValue<EventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, EventHandlerProxy.For<TBaseEvent, TEvent>(eventHandler));

            return false;
        }
    }
}