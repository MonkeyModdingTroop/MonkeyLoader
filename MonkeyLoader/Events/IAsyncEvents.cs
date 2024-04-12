using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Defines the interface for async event data with a
    /// <see cref="Target">target object</see> that's the focus of the async event.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target object that is the focus of the async event.</typeparam>
    public interface IAsyncEvent<out TTarget>
    {
        /// <summary>
        /// Gets the target object that is the focus of the async event.
        /// </summary>
        public TTarget Target { get; }
    }

    /// <summary>
    /// Defines the interface for cancelable async event data with a
    /// <see cref="IAsyncEvent{TTarget}.Target">target object</see> that's the focus of the async event.
    /// </summary>
    /// <inheritdoc/>
    public interface ICancelableAsyncEvent<out TTarget> : IAsyncEvent<TTarget>
    {
        /// <summary>
        /// Gets or sets whether this async event has been canceled by
        /// a previous <see cref="ICancelableAsyncEventHandler{TEvent, TTarget}">async event handler</see>.
        /// </summary>
        /// <remarks>
        /// When this property is <c>true</c>, the default action should be prevented from happening.
        /// </remarks>
        public bool Canceled { get; set; }
    }
}