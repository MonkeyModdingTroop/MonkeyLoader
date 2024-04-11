using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Events
{
    public interface ICancelableEventHandler<in TEvent, out TTarget> : IPrioritizable
        where TEvent : ICancelableEvent<TTarget>
    {
        public bool SkipCanceled { get; }

        public void Handle(TEvent eventArgs);
    }

    public interface IEventHandler<in TEvent, out TTarget> : IPrioritizable
        where TEvent : IEvent<TTarget>
    {
        public void Handle(TEvent eventArgs);
    }
}