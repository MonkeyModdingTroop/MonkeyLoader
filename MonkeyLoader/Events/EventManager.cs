using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed class EventManager
    {
        private readonly AnyMap _eventDispatchers = new();
        private readonly MonkeyLoader _loader;
        internal Logger Logger { get; }

        internal EventManager(MonkeyLoader loader)
        {
            _loader = loader;
            Logger = new(loader.Logger, "EventManager");
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : class
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent>).AddHandler(mod, eventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent>).AddHandler(mod, cancelableEventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : class
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent>).AddHandler(mod, asyncEventHandler);
        }

        internal bool RegisterEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent>).AddHandler(mod, cancelableAsyncEventHandler);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : class
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent>).AddSource(mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent>).AddSource(mod, cancelableEventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : class
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent>).AddSource(mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent>).AddSource(mod, cancelableEventSource);
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableEventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, IEventHandler<TEvent> eventHandler)
            where TEvent : class
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, eventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableAsyncEventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent>(Mod mod, IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : class
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, asyncEventHandler);

            return false;
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, IEventSource<TEvent> eventSource)
            where TEvent : class
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, ICancelableAsyncEventSource<TEvent> cancelableEventSource)
            where TEvent : class, ICancelableEvent
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent>(Mod mod, IAsyncEventSource<TEvent> eventSource)
            where TEvent : class
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        internal void UnregisterMod(Mod mod)
        {
            ValidateLoader(mod);

            foreach (var eventDispatcher in _eventDispatchers.GetCastableValues<IEventDispatcher>())
                eventDispatcher.UnregisterMod(mod);
        }

        private AsyncEventDispatcher<TEvent> CreateAsyncDispatcher<TEvent>()
            where TEvent : class => new(this);

        private CancelableAsyncEventDispatcher<TEvent> CreateCancelableAsyncDispatcher<TEvent>()
            where TEvent : class, ICancelableEvent => new(this);

        private CancelableEventDispatcher<TEvent> CreateCancelableDispatcher<TEvent>()
            where TEvent : class, ICancelableEvent => new(this);

        private EventDispatchers<TEvent> CreateDispatcher<TEvent>()
            where TEvent : class => new(this);

        private void ValidateLoader(Mod mod)
        {
            if (mod.Loader != _loader)
                throw new InvalidOperationException("Can't (un)register event handler of mod from another loader!");
        }
    }
}