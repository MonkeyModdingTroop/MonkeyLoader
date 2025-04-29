using EnumerableToolkit;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Represents the abstract base for the concrete dispatcher types.
    /// </summary>
    /// <typeparam name="TSource">The type of the event sources.</typeparam>
    /// <typeparam name="THandler">The type of the event handlers.</typeparam>
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

        /// <summary>
        /// Creates a new dispatcher with the given details.
        /// </summary>
        /// <param name="manager">The manager that this dispatcher belongs to.</param>
        /// <param name="removeSourceMethod">The generic <c>RemoveSource</c> implementation of the concrete dispatcher.</param>
        protected EventDispatcherBase(EventManager manager, MethodInfo removeSourceMethod)
        {
            _manager = manager;
            _removeSourceMethod = removeSourceMethod;
        }

        /// <summary>
        /// Adds the <paramref name="handler"/> from the given <paramref name="mod"/> to this dispatcher.
        /// </summary>
        /// <param name="mod">The mod that the <paramref name="handler"/> comes from.</param>
        /// <param name="handler">The <typeparamref name="THandler"/> to add.</param>
        /// <returns><c>true</c> if the handler was newly added; otherwise, <c>false</c>.</returns>
        public bool AddHandler(Mod mod, THandler handler)
        {
            if (_handlersByMod.GetOrCreateValue(mod).Add(handler))
            {
                handlers.Add(handler);
                Logger.Debug(() => $"Added handler [{handler.GetType().CompactDescription()}] to event source [{typeof(TSource).CompactDescription()}] for mod: {mod}!");

                return true;
            }

            Logger.Warn(() => $"Tried to add duplicate handler [{handler.GetType().CompactDescription()}] to event source [{typeof(TSource).CompactDescription()}] for mod: {mod}!");

            return false;
        }

        /// <summary>
        /// Removes the <typeparamref name="THandler"/> from the given <paramref name="mod"/> from this dispatcher.
        /// </summary>
        /// <param name="mod">The mod that the <paramref name="handler"/> comes from.</param>
        /// <param name="handler">The <typeparamref name="THandler"/> to remove.</param>
        /// <returns><c>true</c> if the handler was found and removed; otherwise, <c>false</c>.</returns>
        public bool RemoveHandler(Mod mod, THandler handler)
        {
            if (_handlersByMod.TryGetValue(mod, out var modHandlers))
            {
                if (modHandlers.Remove(handler))
                {
                    handlers.Remove(handler);
                    Logger.Debug(() => $"Removed handler [{handler.GetType().CompactDescription()}] from event source [{typeof(TSource).CompactDescription()}] for mod: {mod}!");

                    return true;
                }

                Logger.Warn(() => $"Tried to remove missing handler [{handler.GetType().CompactDescription()}] from event source [{typeof(TSource).CompactDescription()}] for missing mod: {mod}!");

                return false;
            }

            Logger.Warn(() => $"Tried to remove handler [{handler.GetType().CompactDescription()}] from event source [{typeof(TSource).CompactDescription()}] for missing mod: {mod}!");

            return false;
        }

        /// <summary>
        /// Removes all <typeparamref name="TSource"/>s and <typeparamref name="THandler"/>s
        /// of the given <paramref name="mod"/> from this dispatcher.
        /// </summary>
        /// <param name="mod">The mod that the sources and handlers come from.</param>
        public void UnregisterMod(Mod mod)
        {
            Logger.Debug(() => $"Removing all {typeof(TSource).CompactDescription()} sources of mod: {mod}!");

            if (sourcesByMod.TryGetValue(mod, out var modSourcesByType))
            {
                List<Action> sourceRemovals = [];

                foreach (var typeModSources in modSourcesByType)
                {
                    Logger.Debug(() => $"Removing sources of event type: {typeModSources.Key.CompactDescription()}!");

                    var removeSource = _removeSourceMethod.MakeGenericMethod(typeModSources.Key);

                    foreach (var source in typeModSources.Value)
                    {
                        Logger.Trace(() => $"Removing concrete source: {source!.GetType().CompactDescription()}!");

                        sourceRemovals.Add(() => removeSource.Invoke(this, [mod, source]));
                    }
                }

                foreach (var removeSource in sourceRemovals)
                    removeSource();
            }

            sourcesByMod.Remove(mod);
            _handlersByMod.Remove(mod);
        }

        /// <summary>
        /// Adds the <paramref name="source"/> for the <paramref name="eventType"/>
        /// from the given <paramref name="mod"/> to this dispatcher.
        /// </summary>
        /// <param name="mod">The mod that the <paramref name="source"/> comes from.</param>
        /// <param name="eventType">The type of the event that the source is for.</param>
        /// <param name="source">The <typeparamref name="TSource"/> to add.</param>
        /// <returns><c>true</c> if the source was newly added; otherwise, <c>false</c>.</returns>
        protected bool AddSource(Mod mod, Type eventType, TSource source)
        {
            if (sourcesByMod.GetOrCreateValue(mod).GetOrCreateValue(eventType).Add(source))
            {
                Logger.Debug(() => $"Added source [{source?.GetType().CompactDescription()}] for event [{eventType.CompactDescription()}] for mod: {mod}!");

                return true;
            }

            Logger.Warn(() => $"Tried to add duplicate source [{source?.GetType().CompactDescription()}] for event [{eventType.CompactDescription()}] for mod: {mod}!");

            return false;
        }

        /// <summary>
        /// Removes the <paramref name="source"/> for the <paramref name="eventType"/>
        /// from the given <paramref name="mod"/> from this dispatcher.
        /// </summary>
        /// <param name="mod">The mod that the <paramref name="source"/> comes from.</param>
        /// <param name="eventType">The type of the event that the source is for.</param>
        /// <param name="source">The <typeparamref name="TSource"/> to remove.</param>
        /// <returns><c>true</c> if the source was found and removed; otherwise, <c>false</c>.</returns>
        protected bool RemoveSource(Mod mod, Type eventType, TSource source)
        {
            if (sourcesByMod.TryGetValue(mod, out var modSourcesByType))
            {
                if (modSourcesByType.TryGetValue(eventType, out var modTypeSources))
                {
                    if (modTypeSources.Remove(source))
                    {
                        Logger.Debug(() => $"Removed source [{source!.GetType().CompactDescription()}] of event [{eventType.CompactDescription()}] for mod: {mod}!");

                        return true;
                    }

                    Logger.Warn(() => $"Tried to remove missing source [{source!.GetType().CompactDescription()}] of event [{eventType.CompactDescription()}] for mod: {mod}!");

                    return false;
                }

                Logger.Warn(() => $"Tried to remove source [{source!.GetType().CompactDescription()}] of missing event [{eventType.CompactDescription()}] for mod: {mod}!");

                return false;
            }

            Logger.Warn(() => $"Tried to remove source [{source!.GetType().CompactDescription()}] of event [{eventType.CompactDescription()}] for missing mod: {mod}!");

            return false;
        }
    }

    internal interface IEventDispatcher
    {
        public void UnregisterMod(Mod mod);
    }
}