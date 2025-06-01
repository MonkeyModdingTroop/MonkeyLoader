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
        private static readonly MethodInfo _registerAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterAsyncEventSource));
        private static readonly MethodInfo _registerCancelableAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableAsyncEventSource));
        private static readonly MethodInfo _registerCancelableSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterCancelableSyncEventSource));
        private static readonly MethodInfo _registerSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(RegisterSyncEventSource));

        private static readonly MethodInfo _unregisterAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterAsyncEventSource));
        private static readonly MethodInfo _unregisterCancelableAsyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableAsyncEventSource));
        private static readonly MethodInfo _unregisterCancelableSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterCancelableSyncEventSource));
        private static readonly MethodInfo _unregisterSyncEventSource = AccessTools.Method(typeof(EventManager), nameof(UnregisterSyncEventSource));

        internal bool RegisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerAsyncEventSource, mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableAsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _registerCancelableAsyncEventSource, mod, cancelableEventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableSyncEvent
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

        internal bool UnregisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterAsyncEventSource, mod, eventSource);
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableAsyncEvent
        {
            ValidateLoader(mod);

            return InvokeMethodForAllDispatchableEvents(typeof(TEvent), _unregisterCancelableAsyncEventSource, mod, cancelableEventSource);
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableSyncEvent
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

        private bool RegisterAsyncEventSource<TConcreteType, TEvent>(Mod mod, IAsyncEventSource<TConcreteType> eventSource)
            where TConcreteType : TEvent
            where TEvent : AsyncEvent
            => _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent>).AddSource(mod, eventSource);

        private bool RegisterCancelableAsyncEventSource<TConcreteType, TEvent>(Mod mod, ICancelableAsyncEventSource<TConcreteType> cancelableEventSource)
            where TConcreteType : TEvent
            where TEvent : CancelableAsyncEvent
            => _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent>).AddSource(mod, cancelableEventSource);

        private bool RegisterCancelableSyncEventSource<TConcreteType, TEvent>(Mod mod, ICancelableEventSource<TConcreteType> cancelableEventSource)
            where TConcreteType : TEvent
            where TEvent : CancelableSyncEvent
            => _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent>).AddSource(mod, cancelableEventSource);

        private bool RegisterSyncEventSource<TConcreteType, TEvent>(Mod mod, IEventSource<TConcreteType> eventSource)
            where TConcreteType : TEvent
            where TEvent : SyncEvent
            => _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent>).AddSource(mod, eventSource);

        private bool UnregisterAsyncEventSource<TConcreteType, TEvent>(Mod mod, IAsyncEventSource<TConcreteType> eventSource)
            where TConcreteType : TEvent
            where TEvent : AsyncEvent
        {
            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        private bool UnregisterCancelableAsyncEventSource<TConcreteType, TEvent>(Mod mod, ICancelableAsyncEventSource<TConcreteType> cancelableEventSource)
            where TConcreteType : TEvent
            where TEvent : CancelableAsyncEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        private bool UnregisterCancelableSyncEventSource<TConcreteType, TEvent>(Mod mod, ICancelableEventSource<TConcreteType> cancelableEventSource)
            where TConcreteType : TEvent
            where TEvent : CancelableSyncEvent
        {
            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        private bool UnregisterSyncEventSource<TConcreteType, TEvent>(Mod mod, IEventSource<TConcreteType> eventSource)
            where TConcreteType : TEvent
            where TEvent : SyncEvent
        {
            if (_eventDispatchers.TryGetValue<EventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }
    }
}