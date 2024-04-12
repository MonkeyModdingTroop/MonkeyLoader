using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal sealed class CancelableEventDispatcher<TEvent, TTarget>
            : EventDispatcherBase<ICancelableEventSource<TEvent, TTarget>, ICancelableEventHandler<TEvent, TTarget>>
        where TEvent : class, ICancelableEvent<TTarget>
    {
        public CancelableEventDispatcher(EventManager manager) : base(manager)
        { }

        protected override void AddSource(ICancelableEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching += DispatchEvents;

        protected override void RemoveSource(ICancelableEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching -= DispatchEvents;

        private void DispatchEvents(TEvent eventArgs)
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
                    handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(() => ex.Format($"Event handler [{handler.GetType()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }
    }

    internal sealed class EventDispatchers<TEvent, TTarget>
            : EventDispatcherBase<IEventSource<TEvent, TTarget>, IEventHandler<TEvent, TTarget>>
        where TEvent : class, IEvent<TTarget>
    {
        public EventDispatchers(EventManager manager) : base(manager)
        { }

        protected override void AddSource(IEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching += DispatchEvents;

        protected override void RemoveSource(IEventSource<TEvent, TTarget> eventSource)
            => eventSource.Dispatching -= DispatchEvents;

        private void DispatchEvents(TEvent eventArgs)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handle(eventArgs);
                }
                catch (Exception ex)
                {
                    Logger.Warn(() => ex.Format($"Event handler [{handler.GetType()}] threw an exception for event [{eventArgs}]:"));
                }
            }
        }
    }
}