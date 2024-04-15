using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System.Collections.Generic;

namespace MonkeyLoader.Events
{
    internal abstract class EventDispatcherBase<TSource, THandler> : IEventDispatcher
        where THandler : IPrioritizable
    {
        protected readonly SortedCollection<THandler> handlers = new((IComparer<THandler>)Prioritizable.Comparer);

        private readonly Dictionary<Mod, HashSet<THandler>> _handlersByMod = new();
        private readonly EventManager _manager;
        private readonly Dictionary<Mod, HashSet<TSource>> _sourcesByMod = new();

        protected Logger Logger => _manager.Logger;

        protected EventDispatcherBase(EventManager manager)
        {
            _manager = manager;
        }

        public bool AddHandler(Mod mod, THandler handler)
        {
            if (_handlersByMod.GetOrCreateValue(mod).Add(handler))
            {
                handlers.Add(handler);
                return true;
            }

            return false;
        }

        public bool AddSource(Mod mod, TSource source)
        {
            if (_sourcesByMod.GetOrCreateValue(mod).Add(source))
            {
                AddSource(source);
                return true;
            }

            return false;
        }

        public bool RemoveHandler(Mod mod, THandler handler)
        {
            if (_handlersByMod.TryGetValue(mod, out var modHandlers))
            {
                modHandlers.Remove(handler);
                handlers.Remove(handler);

                return true;
            }

            return false;
        }

        public bool RemoveSource(Mod mod, TSource source)
        {
            if (_sourcesByMod.TryGetValue(mod, out var modSources))
            {
                modSources.Remove(source);
                return true;
            }

            return false;
        }

        public void UnregisterMod(Mod mod)
        {
            if (_sourcesByMod.TryGetValue(mod, out var modSources))
            {
                foreach (var source in modSources)
                    RemoveSource(source);
            }

            _sourcesByMod.Remove(mod);

            if (_handlersByMod.TryGetValue(mod, out var modHandlers))
            {
                foreach (var handler in modHandlers)
                    handlers.Remove(handler);
            }

            _handlersByMod.Remove(mod);
        }

        protected abstract void AddSource(TSource eventSource);

        protected abstract void RemoveSource(TSource eventSource);
    }

    internal interface IEventDispatcher
    {
        public void UnregisterMod(Mod mod);
    }
}