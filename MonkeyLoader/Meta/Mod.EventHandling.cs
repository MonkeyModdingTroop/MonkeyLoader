using MonkeyLoader.Configuration;
using MonkeyLoader.Events;
using MonkeyLoader.NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    public abstract partial class Mod : IConfigOwner, IShutdown, ILoadedNuGetPackage, IComparable<Mod>,
        INestedIdentifiableOwner<ConfigSection>, INestedIdentifiableOwner<IDefiningConfigKey>,
        IIdentifiableOwner<Mod, IMonkey>, IIdentifiableOwner<Mod, IEarlyMonkey>
    {
        /// <summary>
        /// Registers the given <see cref="IEventHandler{TEvent}">event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="eventHandler">The <see cref="IEventHandler{TEvent}">event handler</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="eventHandler"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventHandler<TEvent>(IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
            => Loader.EventManager.RegisterEventHandler(this, eventHandler);

        /// <summary>
        /// Registers the given <see cref="ICancelableEventHandler{TEvent}">cancelable event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of cancelable events handled.</typeparam>
        /// <param name="cancelableEventHandler">The <see cref="ICancelableEventHandler{TEvent}">cancelable event handler</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableEventHandler"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventHandler<TEvent>(ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : CancelableSyncEvent
            => Loader.EventManager.RegisterEventHandler(this, cancelableEventHandler);

        /// <summary>
        /// Registers the given <see cref="IAsyncEventHandler{TEvent}">async event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="asyncEventHandler">The <see cref="IAsyncEventHandler{TEvent}">async event handler</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="asyncEventHandler"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventHandler<TEvent>(IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
            => Loader.EventManager.RegisterEventHandler(this, asyncEventHandler);

        /// <summary>
        /// Registers the given <see cref="ICancelableEventHandler{TEvent}">cancelable async event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of cancelable async events handled.</typeparam>
        /// <param name="cancelableAsyncEventHandler">The <see cref="ICancelableEventHandler{TEvent}">cancelable async event handler</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableAsyncEventHandler"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventHandler<TEvent>(ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : CancelableAsyncEvent
            => Loader.EventManager.RegisterEventHandler(this, cancelableAsyncEventHandler);

        /// <summary>
        /// Registers the given <see cref="IEventSource{TEvent}">event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="eventSource">The <see cref="IEventSource{TEvent}">event source</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="eventSource"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventSource<TEvent>(IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
            => Loader.EventManager.RegisterEventSource(this, eventSource);

        /// <summary>
        /// Registers the given <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="cancelableEventSource">The <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableEventSource"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventSource<TEvent>(ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableSyncEvent
            => Loader.EventManager.RegisterEventSource(this, cancelableEventSource);

        /// <summary>
        /// Registers the given <see cref="IAsyncEventSource{TEvent}">event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="eventSource">The <see cref="IAsyncEventSource{TEvent}">event source</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="eventSource"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventSource<TEvent>(IAsyncEventSource<TEvent> eventSource)
            where TEvent : AsyncEvent
            => Loader.EventManager.RegisterEventSource(this, eventSource);

        /// <summary>
        /// Registers the given <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="cancelableAsyncEventSource">The <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> to register.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableAsyncEventSource"/> was newly registered; otherwise, <c>false</c>.</returns>
        public bool RegisterEventSource<TEvent>(ICancelableAsyncEventSource<TEvent> cancelableAsyncEventSource)
            where TEvent : CancelableAsyncEvent
            => Loader.EventManager.RegisterEventSource(this, cancelableAsyncEventSource);

        /// <summary>
        /// Unregisters the given <see cref="IEventHandler{TEvent}">event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="eventHandler">The <see cref="IEventHandler{TEvent}">event handler</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="eventHandler"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventHandler<TEvent>(IEventHandler<TEvent> eventHandler)
            where TEvent : SyncEvent
            => Loader.EventManager.UnregisterEventHandler(this, eventHandler);

        /// <summary>
        /// Unregisters the given <see cref="ICancelableEventHandler{TEvent}">cancelable event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of cancelable events handled.</typeparam>
        /// <param name="cancelableEventHandler">The <see cref="ICancelableEventHandler{TEvent}">cancelable event handler</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableEventHandler"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventHandler<TEvent>(ICancelableEventHandler<TEvent> cancelableEventHandler)
            where TEvent : CancelableSyncEvent
            => Loader.EventManager.UnregisterEventHandler(this, cancelableEventHandler);

        /// <summary>
        /// Unregisters the given <see cref="IAsyncEventHandler{TEvent}">event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="asyncEventHandler">The <see cref="IAsyncEventHandler{TEvent}">event handler</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="asyncEventHandler"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventHandler<TEvent>(IAsyncEventHandler<TEvent> asyncEventHandler)
            where TEvent : AsyncEvent
            => Loader.EventManager.UnregisterEventHandler(this, asyncEventHandler);

        /// <summary>
        /// Unregisters the given <see cref="ICancelableEventHandler{TEvent}">cancelable async event handler</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of cancelable async events handled.</typeparam>
        /// <param name="cancelableAsyncEventHandler">The <see cref="ICancelableEventHandler{TEvent}">cancelable async event handler</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableAsyncEventHandler"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventHandler<TEvent>(ICancelableAsyncEventHandler<TEvent> cancelableAsyncEventHandler)
            where TEvent : CancelableAsyncEvent
            => Loader.EventManager.UnregisterEventHandler(this, cancelableAsyncEventHandler);

        /// <summary>
        /// Unregisters the given <see cref="IEventSource{TEvent}">event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="eventSource">The <see cref="IEventSource{TEvent}">event source</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="eventSource"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventSource<TEvent>(IEventSource<TEvent> eventSource)
            where TEvent : SyncEvent
            => Loader.EventManager.UnregisterEventSource(this, eventSource);

        /// <summary>
        /// Unregisters the given <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of events handled.</typeparam>
        /// <param name="cancelableEventSource">The <see cref="ICancelableEventSource{TEvent}">cancelable event source</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableEventSource"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventSource<TEvent>(ICancelableEventSource<TEvent> cancelableEventSource)
            where TEvent : CancelableSyncEvent
            => Loader.EventManager.UnregisterEventSource(this, cancelableEventSource);

        /// <summary>
        /// Unregisters the given <see cref="IAsyncEventSource{TEvent}">event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="asyncEventSource">The <see cref="IAsyncEventSource{TEvent}">event source</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="asyncEventSource"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventSource<TEvent>(IAsyncEventSource<TEvent> asyncEventSource)
            where TEvent : AsyncEvent
            => Loader.EventManager.UnregisterEventSource(this, asyncEventSource);

        /// <summary>
        /// Unregisters the given <see cref="ICancelableEventSource{TEvent}">cancelable async event source</see> for this mod.
        /// </summary>
        /// <remarks>
        /// Handlers are automatically unregistered when the mod is <see cref="Shutdown">shutdown</see>.
        /// </remarks>
        /// <typeparam name="TEvent">The type of async events handled.</typeparam>
        /// <param name="cancelableAsyncEventSource">The <see cref="ICancelableEventSource{TEvent}">cancelable async event source</see> to unregister.</param>
        /// <returns><c>true</c> if the <paramref name="cancelableAsyncEventSource"/> was found and unregistered; otherwise, <c>false</c>.</returns>
        public bool UnregisterEventSource<TEvent>(ICancelableAsyncEventSource<TEvent> cancelableAsyncEventSource)
            where TEvent : CancelableAsyncEvent
            => Loader.EventManager.UnregisterEventSource(this, cancelableAsyncEventSource);
    }
}