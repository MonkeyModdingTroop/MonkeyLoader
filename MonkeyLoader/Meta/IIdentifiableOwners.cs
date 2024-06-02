using System.Collections.Generic;

namespace MonkeyLoader.Meta
{
    public interface IIdentifiableOwner<out TNestedIdentifiable> : IIdentifiable
        where TNestedIdentifiable : INestedIdentifiable
    {
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }

    public interface IIdentifiableOwner<in TOwner, out TNestedIdentifiable> : IIdentifiableOwner<TNestedIdentifiable>
        where TOwner : IIdentifiableOwner<TOwner, TNestedIdentifiable>
        where TNestedIdentifiable : INestedIdentifiable<TOwner>
    { }

    public interface INestedIdentifiableOwner<out TNestedIdentifiable> : IIdentifiable
        where TNestedIdentifiable : INestedIdentifiable
    {
        public IEnumerable<TNestedIdentifiable> Items { get; }
    }
}