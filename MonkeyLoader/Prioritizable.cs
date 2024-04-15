using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Contains a comparer for <see cref="IPrioritizable"/>.
    /// </summary>
    public static class Prioritizable
    {
        /// <summary>
        /// Gets an <see cref="IComparer{T}"/> that sorts <see cref="IPrioritizable"/> instances
        /// by their <see cref="IPrioritizable.Priority">Priority</see> in descending order.
        /// </summary>
        public static IComparer<IPrioritizable> Comparer { get; } = new PrioritizableComparer();

        private sealed class PrioritizableComparer : IComparer<IPrioritizable>
        {
            public int Compare(IPrioritizable x, IPrioritizable y) => y.Priority - x.Priority;
        }
    }
}