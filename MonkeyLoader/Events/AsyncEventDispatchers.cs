using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed class AsyncEventDispatcher<TEvent, TTarget>
            : EventDispatcherBase<IAsyncEventSource<TEvent, TTarget>, IAsyncEventHandler<TEvent, TTarget>>
        where TEvent : class, IAsyncEvent<TTarget>
    {
        public AsyncEventDispatcher(EventManager manager) : base(manager)
        { }

        protected override void AddSource(IAsyncEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching += DispatchEventsAsync;

        protected override void RemoveSource(IAsyncEventSource<TEvent, TTarget> eventSource)
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

    internal sealed class CancelableAsyncEventDispatcher<TEvent, TTarget>
            : EventDispatcherBase<ICancelableAsyncEventSource<TEvent, TTarget>, ICancelableAsyncEventHandler<TEvent, TTarget>>
        where TEvent : class, ICancelableAsyncEvent<TTarget>
    {
        public CancelableAsyncEventDispatcher(EventManager manager) : base(manager)
        { }

        protected override void AddSource(ICancelableAsyncEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching += DispatchEventsAsync;

        protected override void RemoveSource(ICancelableAsyncEventSource<TEvent, TTarget> eventSource)
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