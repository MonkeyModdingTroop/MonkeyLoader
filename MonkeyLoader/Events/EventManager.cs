using MonkeyLoader.Logging;
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
        private readonly AnyMap _eventDispatchers = new();
        private readonly MonkeyLoader _loader;

        internal Logger Logger { get; }

        internal EventManager(MonkeyLoader loader)
        {
            _loader = loader;
            Logger = new(loader.Logger, "EventManager");
        }

        internal void UnregisterMod(Mod mod)
        {
            ValidateLoader(mod);

            Logger.Info(() => $"Unregistering all event sources and handlers of mod: {mod}");

            try
            {
                foreach (var eventDispatcher in _eventDispatchers.GetCastableValues<IEventDispatcher>())
                    eventDispatcher.UnregisterMod(mod);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.LogFormat($"Error while unregistering mod: {mod}"));
            }

            Logger.Info(() => $"Unregistered all event sources and handlers of mod: {mod}");
        }

        private AsyncEventDispatcher<TEvent> CreateAsyncDispatcher<TEvent>()
            where TEvent : AsyncEvent => new(this);

        private CancelableAsyncEventDispatcher<TEvent> CreateCancelableAsyncDispatcher<TEvent>()
            where TEvent : CancelableAsyncEvent => new(this);

        private CancelableEventDispatcher<TEvent> CreateCancelableDispatcher<TEvent>()
            where TEvent : CancelableSyncEvent => new(this);

        private EventDispatcher<TEvent> CreateDispatcher<TEvent>()
            where TEvent : SyncEvent => new(this);

        private bool InvokeMethodForAllDispatchableEvents(Type concreteEventType, MethodInfo method, params object[] parameters)
        {
            var done = false;

            foreach (var eventType in Event.GetDispatchableEventTypes(concreteEventType))
            {
                done |= (bool)method.MakeGenericMethod(concreteEventType, eventType)
                    .Invoke(this, parameters)!;
            }

            return done;
        }

        private bool InvokeMethodForAllSubscribableEvents(Type concreteEventType, MethodInfo method, params object[] parameters)
        {
            var done = false;

            // Todo: make a generic structure wrapping caching this sort of method access based on type sequences?
            foreach (var eventType in Event.GetSubscribableEventTypes(concreteEventType))
            {
                done |= (bool)method.MakeGenericMethod(eventType, concreteEventType)
                    .Invoke(this, parameters)!;
            }

            return done;
        }

        private void ValidateLoader(Mod mod)
        {
            if (mod.Loader != _loader)
                throw new InvalidOperationException("Can't (un)register event handler of mod from another loader!");
        }
    }
}