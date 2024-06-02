using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    public interface IIdentifiableCollection<out TIdenfiable>
        where TIdenfiable : IIdentifiable
    {
        public IEnumerable<TIdenfiable> Items { get; }
    }

    public interface INestedIdentifiableCollection<out TNestedIdentifiable>
        where TNestedIdentifiable : INestedIdentifiable
    {
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }
}