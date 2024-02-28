using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Contains extension methods for collections of <see cref="IShutdown"/> instances.
    /// </summary>
    public static class ShutdownEnumerableExtensions
    {
        /// <summary>
        /// Calls the <see cref="IShutdown.Shutdown"/> method on all elements of the collection,
        /// aggregating their success state as an 'all'.
        /// </summary>
        /// <param name="shutdowns">The <see cref="IShutdown"/> instances to process.</param>
        /// <returns><c>true</c> if all instances successfully shut down, <c>false</c> otherwise.</returns>
        public static bool ShutdownAll(this IEnumerable<IShutdown> shutdowns)
        {
            var success = true;

            foreach (var shutdown in shutdowns)
                success &= shutdown.Shutdown();

            return success;
        }
    }

    /// <summary>
    /// Interface for everything that can be shut down.
    /// </summary>
    public interface IShutdown
    {
        /// <summary>
        /// Gets whether this object's <see cref="Shutdown">Shutdown</see>() failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; }

        /// <summary>
        /// Gets whether this object's <see cref="Shutdown">Shutdown</see>() method has been called.
        /// </summary>
        public bool ShutdownRan { get; }

        /// <summary>
        /// Lets this object cleanup and shutdown.<br/>
        /// Must only be called once.
        /// </summary>
        /// <returns>Whether it ran successfully.</returns>
        /// <exception cref="InvalidOperationException">If it gets called more than once.</exception>
        public bool Shutdown();
    }
}