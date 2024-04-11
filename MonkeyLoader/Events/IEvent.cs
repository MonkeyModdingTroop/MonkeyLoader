using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    public interface ICancelableEvent<out TTarget> : IEvent<TTarget>
    {
        public bool Canceled { get; set; }
    }

    public interface IEvent<out TTarget>
    {
        public TTarget Target { get; }
    }
}