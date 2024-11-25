using MonkeyLoader.Components;
using MonkeyLoader.Meta;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains constants and extensions for <see cref="ConfigKeyBidirectionalBinding{T}"/>.
    /// </summary>
    public static class ConfigKeyBindings
    {
        /// <summary>
        /// The base event label used when a config item's value is set from a
        /// <see cref="IConfigKeyBidirectionalBinding{T}.Owner"/>'s changed value being propagated.
        /// </summary>
        /// <remarks>
        /// The actual event label will have the format:
        /// <c>BidirectionalBindingOwner:<see cref="IIdentifiable.FullId">TriggerFullId</see>:<see cref="IConfigKeyChangedEventArgs.Label">TriggerLabel</see></c>.
        /// </remarks>
        public const string SetFromBidirectionalOwnerEventLabel = "BidirectionalBindingOwner";

        /// <summary>
        /// The base event label used when a config item's value is set from a
        /// <see cref="IConfigKeyBidirectionalBinding{T}.Target"/>'s changed value being propagated.
        /// </summary>
        /// <remarks>
        /// The actual event label will have the format:
        /// <c>BidirectionalBindingTarget:<see cref="IIdentifiable.FullId">TriggerFullId</see>:<see cref="IConfigKeyChangedEventArgs.Label">TriggerLabel</see></c>.
        /// </remarks>
        public const string SetFromBidirectionalTargetEventLabel = "BidirectionalBindingTarget";

        /// <summary>
        /// Creates a new bidirectional binding that propagates changes between the two config items.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="owner">The config item that the component will be initialized to.</param>
        /// <param name="target">The config item to propagate changes to and from.</param>
        /// <returns>The newly created bidirectional binding component.</returns>
        public static IConfigKeyBidirectionalBinding<T> BindBidirectionallyTo<T>(this IDefiningConfigKey<T> owner, IDefiningConfigKey<T> target)
        {
            var binding = new ConfigKeyBidirectionalBinding<T>(target);

            owner.Add(binding);

            return binding;
        }
    }
}