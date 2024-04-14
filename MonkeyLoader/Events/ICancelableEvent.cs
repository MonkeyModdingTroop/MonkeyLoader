namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for the data of a cancelable event.
    /// </summary>
    /// <inheritdoc/>
    public interface ICancelableEvent
    {
        /// <summary>
        /// Gets or sets whether this event has been canceled by a previous event handler.
        /// </summary>
        /// <remarks>
        /// When this property is <c>true</c>, the default action should be prevented from happening.
        /// </remarks>
        public bool Canceled { get; set; }
    }
}