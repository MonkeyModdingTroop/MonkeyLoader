using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents the functionality for an <see cref="IDefiningConfigKey{T}"/>,
    /// which propagate any changes in value in both directions
    /// between the two associated config items.
    /// </summary>
    /// <inheritdoc cref="IConfigKeyBidirectionalBinding{T}"/>
    public sealed class ConfigKeyBidirectionalBinding<T> : IConfigKeyBidirectionalBinding<T>
    {
        /// <inheritdoc/>
        public IDefiningConfigKey<T> Owner { get; private set; } = null!;

        IDefiningConfigKey IConfigKeyBidirectionalBinding.Owner => Owner;

        /// <inheritdoc/>
        public IDefiningConfigKey<T> Target { get; }

        IDefiningConfigKey IConfigKeyBidirectionalBinding.Target => Target;

        /// <summary>
        /// Creates a new bidirectional binding targeting the given config item.
        /// </summary>
        /// <param name="target">The other config item to propagate changes to and from.</param>
        public ConfigKeyBidirectionalBinding(IDefiningConfigKey<T> target)
        {
            Target = target;
        }

        /// <remarks>
        /// Adds the <see cref="IDefiningConfigKey{T}.Changed">Changed</see> event
        /// listeners to propagate changes between the linked config items.
        /// </remarks>
        /// <exception cref="InvalidOperationException">When the binding has already been initialized or is targeted at itself.</exception>
        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey<T> entity)
        {
            if (Owner is not null)
                throw new InvalidOperationException($"This binding targetting [{Target}] is already owned by [{Owner}]!");

            if (ReferenceEquals(Target, entity))
                throw new InvalidOperationException($"Can't bind [{Target}] to itself!");

            Owner = entity;

            // Shouldn't need circular check because Changed event is only fired for actual changes
            Owner.Changed += (_, args) => Target.SetValue(args.NewValue!, args.GetPropagatedEventLabel(ConfigKeyBindings.SetFromBidirectionalOwnerEventLabel));
            Target.Changed += (_, args) => Owner.SetValue(args.NewValue!, args.GetPropagatedEventLabel(ConfigKeyBindings.SetFromBidirectionalTargetEventLabel));
        }
    }

    /// <typeparam name="T">The type of the config item's value.</typeparam>
    /// <inheritdoc cref="IConfigKeyBidirectionalBinding"/>
    public interface IConfigKeyBidirectionalBinding<T> : IConfigKeyComponent<IDefiningConfigKey<T>>, IConfigKeyBidirectionalBinding
    {
        /// <inheritdoc cref="IConfigKeyBidirectionalBinding.Owner"/>
        public new IDefiningConfigKey<T> Owner { get; }

        /// <inheritdoc cref="IConfigKeyBidirectionalBinding.Target"/>
        public new IDefiningConfigKey<T> Target { get; }
    }

    /// <summary>
    /// Defines the interface for config key components,
    /// which propagate any changes in value in both directions
    /// between the two associated config items.
    /// </summary>
    public interface IConfigKeyBidirectionalBinding
    {
        /// <summary>
        /// Gets the config item that this component was initialized to.
        /// </summary>
        public IDefiningConfigKey Owner { get; }

        /// <summary>
        /// Gets the config item that this binding targets.
        /// </summary>
        public IDefiningConfigKey Target { get; }
    }
}