using HarmonyLib;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed class AsyncEventDispatcher<TEvent>
            : EventDispatcherBase<IAsyncEventSource<TEvent>, IAsyncEventHandler<TEvent>>
        where TEvent : AsyncEvent
    {
        public AsyncEventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(AsyncEventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        public bool AddSource<TDerivedEvent>(Mod mod, IAsyncEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching += eventDispatcher;

            return true;
        }

        public bool RemoveSource<TDerivedEvent>(Mod mod, IAsyncEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching -= eventDispatcher;

            return true;
        }

        private async Task DispatchEventsAsync(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.LogFormat($"Event handler [{handler.GetType().CompactDescription()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }

        private AsyncEventDispatching<TDerivedEvent> MakeEventDispatcher<TDerivedEvent>()
            where TDerivedEvent : TEvent => new(DispatchEventsAsync);
    }

    internal sealed class CancelableAsyncEventDispatcher<TEvent>
            : EventDispatcherBase<ICancelableAsyncEventSource<TEvent>, ICancelableAsyncEventHandler<TEvent>>
        where TEvent : CancelableAsyncEvent
    {
        public CancelableAsyncEventDispatcher(EventManager manager)
            : base(manager, AccessTools.DeclaredMethod(typeof(CancelableAsyncEventDispatcher<TEvent>), nameof(RemoveSource)))
        { }

        public bool AddSource<TDerivedEvent>(Mod mod, ICancelableAsyncEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!AddSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            // Have to wrap the DispatchEvents method in the correct delegate type,
            // otherwise the event will throw when adding it, despite being compatible
            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching += eventDispatcher;

            return true;
        }

        public bool RemoveSource<TDerivedEvent>(Mod mod, ICancelableAsyncEventSource<TDerivedEvent> eventSource)
            where TDerivedEvent : TEvent
        {
            if (!RemoveSource(mod, typeof(TDerivedEvent), eventSource))
                return false;

            var eventDispatcher = eventDispatchers.GetOrCreateValue(MakeEventDispatcher<TDerivedEvent>);
            eventSource.Dispatching -= eventDispatcher;

            return true;
        }

        private async Task DispatchEventsAsync(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                if (eventArgs.Canceled && handler.SkipCanceled)
                {
                    Logger.Trace(() => $"Skipping event handler [{handler.GetType().CompactDescription()}] for canceled event [{eventArgs}]!");
                    continue;
                }

                try
                {
                    await handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.LogFormat($"Event handler [{handler.GetType().CompactDescription()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }

        private CancelableAsyncEventDispatching<TDerivedEvent> MakeEventDispatcher<TDerivedEvent>()
            where TDerivedEvent : TEvent => new(DispatchEventsAsync);
    }
}