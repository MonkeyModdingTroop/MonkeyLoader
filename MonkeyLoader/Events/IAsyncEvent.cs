namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks an event type as async for <see cref="IAsyncEventSource{TEvent}"/>s and <see cref="IAsyncEventHandler{TEvent}"/>.
    /// </summary>
    public interface IAsyncEvent
    { }
}