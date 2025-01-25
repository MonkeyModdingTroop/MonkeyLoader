using System.Collections.Specialized;
using System;
using System.Diagnostics.CodeAnalysis;
using MonkeyLoader.Meta;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// The delegate that is called for configuration change events.
    /// </summary>
    /// <param name="sender">The object that sent the event.</param>
    /// <param name="configKeyChangedEventArgs">The event containing details about the change.</param>
    public delegate void ConfigKeyChangedEventHandler(object sender, IConfigKeyChangedEventArgs configKeyChangedEventArgs);

    /// <summary>
    /// The delegate that is called for a <see cref="DefiningConfigKey{T}"/>'s <see cref="DefiningConfigKey{T}.Changed">changed event</see>.
    /// </summary>
    /// <typeparam name="T">The type of the key's value.</typeparam>
    /// <param name="sender">The object that sent the event.</param>
    /// <param name="configKeyChangedEventArgs">The event containing details about the change.</param>
    public delegate void ConfigKeyChangedEventHandler<T>(object sender, ConfigKeyChangedEventArgs<T> configKeyChangedEventArgs);

    /// <summary>
    /// Represents the data for the <see cref="Config.ItemChanged"/> and <see cref="MonkeyLoader.AnyConfigChanged"/> events.
    /// </summary>
    /// <typeparam name="T">The type of the key's value.</typeparam>
    public sealed class ConfigKeyChangedEventArgs<T> : ValueChangedEventArgs<T>, IConfigKeyChangedEventArgs
    {
        /// <inheritdoc/>
        public Config Config { get; }

        /// <inheritdoc/>
        public bool HadValue { get; }

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(Label))]
        public bool HasLabel => Label is not null;

        /// <inheritdoc/>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the configuration item who's value changed.
        /// </summary>
        public IDefiningConfigKey<T> Key { get; }

        IDefiningConfigKey IConfigKeyChangedEventArgs.Key => Key;

        /// <inheritdoc/>
        public string? Label { get; }

        /// <summary>
        /// Creates a new event args instance for a changed config item.
        /// </summary>
        /// <param name="config">The config the item belongs to.</param>
        /// <param name="key">The config item that changed.</param>
        /// <param name="hadValue">Whether the config item had a value before the change.</param>
        /// <param name="oldValue">The optional old value.</param>
        /// <param name="hasValue">Whether the config item has a value now.</param>
        /// <param name="newValue">The optional new value.</param>
        /// <param name="label">A custom label assigned to the change.</param>
        /// <param name="changedProperty">The name of the changed property on the value.</param>
        /// <param name="changedCollection">The collection change arguments for the value.</param>
        public ConfigKeyChangedEventArgs(Config config, IDefiningConfigKey<T> key,
            bool hadValue, T? oldValue, bool hasValue, T? newValue, string? label,
            string? changedProperty, NotifyCollectionChangedEventArgs? changedCollection)
                : base(oldValue, newValue, changedProperty, changedCollection)
        {
            Config = config;
            Key = key;

            HadValue = hadValue;
            HasValue = hasValue;

            Label = label;
        }
    }

    /// <summary>
    /// Represents a non-generic <see cref="ConfigKeyChangedEventArgs{T}"/>.
    /// </summary>
    public interface IConfigKeyChangedEventArgs : IValueChangedEventArgs
    {
        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> in which the change occured.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets whether the old value existed.
        /// </summary>
        public bool HadValue { get; }

        /// <summary>
        /// Gets whether a custom label was set by whoever changed the configuration.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Label">Label</see> is not <c>null</c>; otherwise, <c>false</c>.
        /// </value>
        [MemberNotNullWhen(true, nameof(Label))]
        public bool HasLabel { get; }

        /// <summary>
        /// Gets whether the new value exists.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the configuration item who's value changed.
        /// </summary>
        public IDefiningConfigKey Key { get; }

        /// <summary>
        /// Gets a custom label that may be set by whoever changed the configuration.
        /// </summary>
        public string? Label { get; }
    }
}