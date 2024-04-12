﻿using MonkeyLoader.Logging;
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

        internal bool RegisterEventHandler<TEvent, TTarget>(Mod mod, IEventHandler<TEvent, TTarget> eventHandler)
            where TEvent : class, IEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent, TTarget>).AddHandler(mod, eventHandler);
        }

        internal bool RegisterEventHandler<TEvent, TTarget>(Mod mod, ICancelableEventHandler<TEvent, TTarget> cancelableEventHandler)
            where TEvent : class, ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent, TTarget>).AddHandler(mod, cancelableEventHandler);
        }

        internal bool RegisterEventHandler<TEvent, TTarget>(Mod mod, IAsyncEventHandler<TEvent, TTarget> asyncEventHandler)
            where TEvent : class, IAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent, TTarget>).AddHandler(mod, asyncEventHandler);
        }

        internal bool RegisterEventHandler<TEvent, TTarget>(Mod mod, ICancelableAsyncEventHandler<TEvent, TTarget> cancelableAsyncEventHandler)
            where TEvent : class, ICancelableAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent, TTarget>).AddHandler(mod, cancelableAsyncEventHandler);
        }

        internal bool RegisterEventSource<TEvent, TTarget>(Mod mod, IEventSource<TEvent, TTarget> eventSource)
            where TEvent : class, IEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent, TTarget>).AddSource(mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent, TTarget>(Mod mod, ICancelableEventSource<TEvent, TTarget> cancelableEventSource)
            where TEvent : class, ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent, TTarget>).AddSource(mod, cancelableEventSource);
        }

        internal bool RegisterEventSource<TEvent, TTarget>(Mod mod, IAsyncEventSource<TEvent, TTarget> eventSource)
            where TEvent : class, IAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateAsyncDispatcher<TEvent, TTarget>).AddSource(mod, eventSource);
        }

        internal bool RegisterEventSource<TEvent, TTarget>(Mod mod, ICancelableAsyncEventSource<TEvent, TTarget> cancelableEventSource)
            where TEvent : class, ICancelableAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            return _eventDispatchers.GetOrCreateValue(CreateCancelableAsyncDispatcher<TEvent, TTarget>).AddSource(mod, cancelableEventSource);
        }

        internal bool UnregisterEventHandler<TEvent, TTarget>(Mod mod, ICancelableEventHandler<TEvent, TTarget> cancelableEventHandler)
            where TEvent : class, ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent, TTarget>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableEventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent, TTarget>(Mod mod, IEventHandler<TEvent, TTarget> eventHandler)
            where TEvent : class, IEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent, TTarget>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, eventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent, TTarget>(Mod mod, ICancelableAsyncEventHandler<TEvent, TTarget> cancelableAsyncEventHandler)
            where TEvent : class, ICancelableAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent, TTarget>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveHandler(mod, cancelableAsyncEventHandler);

            return false;
        }

        internal bool UnregisterEventHandler<TEvent, TTarget>(Mod mod, IAsyncEventHandler<TEvent, TTarget> asyncEventHandler)
            where TEvent : class, IAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent, TTarget>>(out var eventDispatcher))
                return eventDispatcher!.RemoveHandler(mod, asyncEventHandler);

            return false;
        }

        internal bool UnregisterEventSource<TEvent, TTarget>(Mod mod, ICancelableEventSource<TEvent, TTarget> cancelableEventSource)
            where TEvent : class, ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableEventDispatcher<TEvent, TTarget>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent, TTarget>(Mod mod, IEventSource<TEvent, TTarget> eventSource)
            where TEvent : class, IEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<EventDispatchers<TEvent, TTarget>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent, TTarget>(Mod mod, ICancelableAsyncEventSource<TEvent, TTarget> cancelableEventSource)
            where TEvent : class, ICancelableAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<CancelableAsyncEventDispatcher<TEvent, TTarget>>(out var cancelableEventDispatcher))
                return cancelableEventDispatcher!.RemoveSource(mod, cancelableEventSource);

            return false;
        }

        internal bool UnregisterEventSource<TEvent, TTarget>(Mod mod, IAsyncEventSource<TEvent, TTarget> eventSource)
            where TEvent : class, IAsyncEvent<TTarget>
        {
            ValidateLoader(mod);

            if (_eventDispatchers.TryGetValue<AsyncEventDispatcher<TEvent, TTarget>>(out var eventDispatcher))
                return eventDispatcher!.RemoveSource(mod, eventSource);

            return false;
        }

        internal void UnregisterMod(Mod mod)
        {
            ValidateLoader(mod);

            foreach (var eventDispatcher in _eventDispatchers.GetCastableValues<IEventDispatcher>())
                eventDispatcher.UnregisterMod(mod);
        }

        private AsyncEventDispatcher<TEvent, TTarget> CreateAsyncDispatcher<TEvent, TTarget>()
            where TEvent : class, IAsyncEvent<TTarget> => new(this);

        private CancelableAsyncEventDispatcher<TEvent, TTarget> CreateCancelableAsyncDispatcher<TEvent, TTarget>()
            where TEvent : class, ICancelableAsyncEvent<TTarget> => new(this);

        private CancelableEventDispatcher<TEvent, TTarget> CreateCancelableDispatcher<TEvent, TTarget>()
                            where TEvent : class, ICancelableEvent<TTarget> => new(this);

        private EventDispatchers<TEvent, TTarget> CreateDispatcher<TEvent, TTarget>()
            where TEvent : class, IEvent<TTarget> => new(this);

        private void ValidateLoader(Mod mod)
        {
            if (mod.Loader != _loader)
                throw new InvalidOperationException("Can't (un)register event handler of mod from another loader!");
        }
    }
}