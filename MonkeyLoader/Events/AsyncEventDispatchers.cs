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
        public AsyncEventDispatcher(EventManager manager) : base(manager)
        { }

        protected override void AddSource(IAsyncEventSource<TEvent> eventSource)
            => eventSource.Dispatching += DispatchEventsAsync;

        protected override void RemoveSource(IAsyncEventSource<TEvent> eventSource)
            => eventSource.Dispatching -= DispatchEventsAsync;

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
                    Logger.Warn(() => ex.Format($"Event handler [{handler.GetType()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }
    }

    internal sealed class CancelableAsyncEventDispatcher<TEvent>
            : EventDispatcherBase<ICancelableAsyncEventSource<TEvent>, ICancelableAsyncEventHandler<TEvent>>
        where TEvent : AsyncEvent, ICancelableEvent
    {
        public CancelableAsyncEventDispatcher(EventManager manager) : base(manager)
        { }

        protected override void AddSource(ICancelableAsyncEventSource<TEvent> eventSource)
            => eventSource.Dispatching += DispatchEventsAsync;

        protected override void RemoveSource(ICancelableAsyncEventSource<TEvent> eventSource)
            => eventSource.Dispatching -= DispatchEventsAsync;

        private async Task DispatchEventsAsync(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                if (eventArgs.Canceled && handler.SkipCanceled)
                {
                    Logger.Trace(() => $"Skipping event handler [{handler.GetType()}] for canceled event [{eventArgs}]!");
                    continue;
                }

                try
                {
                    await handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(() => ex.Format($"Event handler [{handler.GetType()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }
    }
}