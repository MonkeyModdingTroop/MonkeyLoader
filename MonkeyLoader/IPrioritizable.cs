using MonkeyLoader.Events;

namespace MonkeyLoader
{
    /// <summary>
    /// Defines the interface for prioritizable things,
    /// primarily the different <see cref="IEventHandler{TEvent}">event handlers</see>.
    /// </summary>
    public interface IPrioritizable
    {
        /// <summary>
        /// Gets the priority of this item. Use the <see cref="HarmonyLib.Priority"/> values as a base.
        /// </summary>
        /// <value>
        /// An interger used to sort the prioritizable items.<br/>
        /// Higher comes first with the <see cref="Prioritizable.Comparer">default comparer</see>.
        /// </value>
        public int Priority { get; }
    }
}