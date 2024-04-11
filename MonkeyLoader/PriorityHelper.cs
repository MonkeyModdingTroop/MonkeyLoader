using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    public static class PriorityHelper
    {
        public static IComparer<IPrioritizable> Comparer { get; } = new PrioritizableComparer();

        private sealed class PrioritizableComparer : IComparer<IPrioritizable>
        {
            public int Compare(IPrioritizable x, IPrioritizable y) => y.Priority - x.Priority;
        }
    }
}