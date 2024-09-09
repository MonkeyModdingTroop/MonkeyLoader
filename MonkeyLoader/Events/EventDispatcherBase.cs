using EnumerableToolkit;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MonkeyLoader.Events
{
    internal abstract class EventDispatcherBase<TSource, THandler> : IEventDispatcher
        where THandler : class, IPrioritizable
    {
        protected readonly AnyMap eventDispatchers = new();
        protected readonly PrioritySortedCollection<THandler> handlers = [];
        protected readonly Dictionary<Mod, Dictionary<Type, HashSet<TSource>>> sourcesByMod = [];
        private readonly Dictionary<Mod, HashSet<THandler>> _handlersByMod = [];
        private readonly EventManager _manager;
        private readonly MethodInfo _removeSourceMethod;
        protected Logger Logger => _manager.Logger;

        protected EventDispatcherBase(EventManager manager, MethodInfo removeSourceMethod)
        {
            _manager = manager;
            _removeSourceMethod = removeSourceMethod;
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

        public void UnregisterMod(Mod mod)
        {
            if (sourcesByMod.TryGetValue(mod, out var modSourcesByType))
            {
                foreach (var typeModSources in modSourcesByType)
                {
                    var removeSource = _removeSourceMethod.MakeGenericMethod(typeModSources.Key);

                    foreach (var source in typeModSources.Value)
                        removeSource.Invoke(this, [mod, source]);
                }
            }

            sourcesByMod.Remove(mod);
            _handlersByMod.Remove(mod);
        }

        protected bool AddSource(Mod mod, Type eventType, TSource source)
            => sourcesByMod.GetOrCreateValue(mod).GetOrCreateValue(eventType).Add(source);

        protected bool RemoveSource(Mod mod, Type eventType, TSource source)
        {
            if (sourcesByMod.TryGetValue(mod, out var modSourcesByType))
            {
                if (modSourcesByType.TryGetValue(eventType, out var modTypeSources))
                    modTypeSources.Remove(source);
                return true;
            }

            return false;
        }
    }

    internal interface IEventDispatcher
    {
        public void UnregisterMod(Mod mod);
    }
}