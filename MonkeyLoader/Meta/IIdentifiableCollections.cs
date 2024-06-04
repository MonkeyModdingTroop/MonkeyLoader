using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for root objects that are the (indirect) parents of <see cref="IIdentifiable"/> items.
    /// </summary>
    /// <typeparam name="TIdentifiable">The type of the <see cref="IIdentifiable"/> items.</typeparam>
    public interface IIdentifiableCollection<out TIdentifiable>
        where TIdentifiable : IIdentifiable
    {
        /// <summary>
        /// Gets the <see cref="IIdentifiable"/> items.
        /// </summary>
        public IEnumerable<TIdentifiable> Items { get; }
    }

    /// <summary>
    /// Defines the interface for root objects that are the (indirect) parents of <see cref="INestedIdentifiable"/> items.
    /// </summary>
    /// <typeparam name="TNestedIdentifiable">The type of the <see cref="INestedIdentifiable"/> items.</typeparam>
    public interface INestedIdentifiableCollection<out TNestedIdentifiable>
        where TNestedIdentifiable : INestedIdentifiable
    {
        /// <summary>
        /// Gets the <see cref="INestedIdentifiable"/> items.
        /// </summary>
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }
}