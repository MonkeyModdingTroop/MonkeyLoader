using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    /// <summary>
    /// Marks an <see cref="Event"/>-derived class as to be dispatched,
    /// even if it's only a base class of the concrete event coming from the source.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DispatchableBaseEventAttribute : MonkeyLoaderAttribute
    { }
}