using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    public interface ICancelableEventSource<out TEvent, out TTarget> where TEvent : ICancelableEvent<TTarget>
    {
        public event Action<TEvent> Dispatched;
    }

    public interface IEventSource<out TEvent, out TTarget> where TEvent : IEvent<TTarget>
    {
        public event Action<TEvent> Dispatched;
    }
}