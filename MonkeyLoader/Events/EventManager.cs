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

        internal void RegisterEventHandler<TEvent, TTarget>(Mod mod, IEventHandler<TEvent, TTarget> eventHandler)
            where TEvent : IEvent<TTarget>
        {
            ValidateLoader(mod);

            _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent, TTarget>).AddHandler(mod, eventHandler);
        }

        internal void RegisterEventHandler<TEvent, TTarget>(Mod mod, ICancelableEventHandler<TEvent, TTarget> cancelableEventHandler)
            where TEvent : ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent, TTarget>).AddHandler(mod, cancelableEventHandler);
        }

        internal void RegisterEventSource<TEvent, TTarget>(Mod mod, IEventSource<TEvent, TTarget> eventSource)
            where TEvent : IEvent<TTarget>
        {
            ValidateLoader(mod);

            _eventDispatchers.GetOrCreateValue(CreateDispatcher<TEvent, TTarget>).AddSource(mod, eventSource);
        }

        internal void RegisterEventSource<TEvent, TTarget>(Mod mod, ICancelableEventSource<TEvent, TTarget> cancelableEventSource)
            where TEvent : ICancelableEvent<TTarget>
        {
            ValidateLoader(mod);

            _eventDispatchers.GetOrCreateValue(CreateCancelableDispatcher<TEvent, TTarget>).AddSource(mod, cancelableEventSource);
        }

        internal void UnregisterMod(Mod mod)
        {
            ValidateLoader(mod);

            foreach (var eventDispatcher in _eventDispatchers.GetCastableValues<IEventDispatcher>())
                eventDispatcher.UnregisterMod(mod);
        }

        private CancelableEventDispatcher<TEvent, TTarget> CreateCancelableDispatcher<TEvent, TTarget>()
            where TEvent : ICancelableEvent<TTarget> => new(this);

        private EventDispatcher<TEvent, TTarget> CreateDispatcher<TEvent, TTarget>()
            where TEvent : IEvent<TTarget> => new(this);

        private void ValidateLoader(Mod mod)
        {
            if (mod.Loader != _loader)
                throw new InvalidOperationException("Can't register event handler of mod from another loader!");
        }
    }
}