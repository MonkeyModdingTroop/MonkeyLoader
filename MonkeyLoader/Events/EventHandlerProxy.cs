using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    internal static class EventHandlerProxy
    {
        public static IEventHandler<TBaseEvent> For<TBaseEvent, TEvent>(IEventHandler<TEvent> eventHandler)
                where TBaseEvent : SyncEvent
                where TEvent : TBaseEvent
            => SyncProxy<TBaseEvent, TEvent>.For(eventHandler);

        public static ICancelableEventHandler<TBaseEvent> For<TBaseEvent, TEvent>(ICancelableEventHandler<TEvent> eventHandler)
                where TBaseEvent : CancelableSyncEvent
                where TEvent : TBaseEvent
            => CancelableSyncProxy<TBaseEvent, TEvent>.For(eventHandler);

        public static IAsyncEventHandler<TBaseEvent> For<TBaseEvent, TEvent>(IAsyncEventHandler<TEvent> eventHandler)
                where TBaseEvent : AsyncEvent
                where TEvent : TBaseEvent
            => AsyncProxy<TBaseEvent, TEvent>.For(eventHandler);

        public static ICancelableAsyncEventHandler<TBaseEvent> For<TBaseEvent, TEvent>(ICancelableAsyncEventHandler<TEvent> eventHandler)
                where TBaseEvent : CancelableAsyncEvent
                where TEvent : TBaseEvent
            => CancelableAsyncProxy<TBaseEvent, TEvent>.For(eventHandler);

        private sealed class AsyncProxy<TBaseEvent, TEvent> : IAsyncEventHandler<TBaseEvent>
            where TBaseEvent : AsyncEvent
            where TEvent : TBaseEvent
        {
            private static readonly ConditionalWeakTable<IAsyncEventHandler<TEvent>, AsyncProxy<TBaseEvent, TEvent>> _proxiesByHandler = new();

            private readonly IAsyncEventHandler<TEvent> _handler;

            public int Priority => _handler.Priority;

            private AsyncProxy(IAsyncEventHandler<TEvent> handler)
            {
                _handler = handler;
            }

            public static AsyncProxy<TBaseEvent, TEvent> For(IAsyncEventHandler<TEvent> handler)
            {
                if (!_proxiesByHandler.TryGetValue(handler, out var proxy))
                {
                    proxy = new(handler);
                    _proxiesByHandler.Add(handler, proxy);
                }

                return proxy;
            }

            public async Task Handle(TBaseEvent eventData)
            {
                if (eventData is TEvent concreteEvent)
                    await _handler.Handle(concreteEvent);
            }
        }

        private sealed class CancelableAsyncProxy<TBaseEvent, TEvent> : ICancelableAsyncEventHandler<TBaseEvent>
            where TBaseEvent : CancelableAsyncEvent
            where TEvent : TBaseEvent
        {
            private static readonly ConditionalWeakTable<ICancelableAsyncEventHandler<TEvent>, CancelableAsyncProxy<TBaseEvent, TEvent>> _proxiesByHandler = new();

            private readonly ICancelableAsyncEventHandler<TEvent> _handler;

            public int Priority => _handler.Priority;

            public bool SkipCanceled => _handler.SkipCanceled;

            private CancelableAsyncProxy(ICancelableAsyncEventHandler<TEvent> handler)
            {
                _handler = handler;
            }

            public static CancelableAsyncProxy<TBaseEvent, TEvent> For(ICancelableAsyncEventHandler<TEvent> handler)
            {
                if (!_proxiesByHandler.TryGetValue(handler, out var proxy))
                {
                    proxy = new(handler);
                    _proxiesByHandler.Add(handler, proxy);
                }

                return proxy;
            }

            public async Task Handle(TBaseEvent eventData)
            {
                if (eventData is TEvent concreteEvent)
                    await _handler.Handle(concreteEvent);
            }
        }

        private sealed class CancelableSyncProxy<TBaseEvent, TEvent> : ICancelableEventHandler<TBaseEvent>
            where TBaseEvent : CancelableSyncEvent
            where TEvent : TBaseEvent
        {
            private static readonly ConditionalWeakTable<ICancelableEventHandler<TEvent>, CancelableSyncProxy<TBaseEvent, TEvent>> _proxiesByHandler = new();

            private readonly ICancelableEventHandler<TEvent> _handler;

            public int Priority => _handler.Priority;

            public bool SkipCanceled => _handler.SkipCanceled;

            private CancelableSyncProxy(ICancelableEventHandler<TEvent> handler)
            {
                _handler = handler;
            }

            public static CancelableSyncProxy<TBaseEvent, TEvent> For(ICancelableEventHandler<TEvent> handler)
            {
                if (!_proxiesByHandler.TryGetValue(handler, out var proxy))
                {
                    proxy = new(handler);
                    _proxiesByHandler.Add(handler, proxy);
                }

                return proxy;
            }

            public void Handle(TBaseEvent eventData)
            {
                if (eventData is TEvent concreteEvent)
                    _handler.Handle(concreteEvent);
            }
        }

        private sealed class SyncProxy<TBaseEvent, TEvent> : IEventHandler<TBaseEvent>
            where TBaseEvent : SyncEvent
            where TEvent : TBaseEvent
        {
            private static readonly ConditionalWeakTable<IEventHandler<TEvent>, SyncProxy<TBaseEvent, TEvent>> _proxiesByHandler = new();

            private readonly IEventHandler<TEvent> _handler;

            public int Priority => _handler.Priority;

            private SyncProxy(IEventHandler<TEvent> handler)
            {
                _handler = handler;
            }

            public static SyncProxy<TBaseEvent, TEvent> For(IEventHandler<TEvent> handler)
            {
                if (!_proxiesByHandler.TryGetValue(handler, out var proxy))
                {
                    proxy = new(handler);
                    _proxiesByHandler.Add(handler, proxy);
                }

                return proxy;
            }

            public void Handle(TBaseEvent eventData)
            {
                if (eventData is TEvent concreteEvent)
                    _handler.Handle(concreteEvent);
            }
        }
    }
}