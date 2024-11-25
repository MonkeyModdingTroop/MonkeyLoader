using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Contains extensions methods related to the config system.
    /// </summary>
    public static class ConfigSystemExtensions
    {
        /// <summary>
        /// Gets the <see cref="IConfigKeyChangedEventArgs.Label">Label</see> for
        /// a propagated <see cref="IDefiningConfigKey.Changed">Changed</see> event.<br/>
        /// The created event label will have this format:
        /// <c>$"{<paramref name="baseLabel"/>}:{<paramref name="changedEventArgs"/>.<see cref="IConfigKeyChangedEventArgs.Key">Key</see>.<see cref="IIdentifiable.FullId">FullId</see>}:{<paramref name="changedEventArgs"/>.<see cref="IConfigKeyChangedEventArgs.Label">Label</see>}"</c>.
        /// </summary>
        /// <param name="changedEventArgs">The changed event that triggered this propagation.</param>
        /// <param name="baseLabel">The new base label for the event.</param>
        /// <returns>The formatted label for the propagated event.</returns>
        public static string GetPropagatedEventLabel(this IConfigKeyChangedEventArgs changedEventArgs, string baseLabel)
            => $"{baseLabel}:{changedEventArgs.Key.FullId}:{changedEventArgs.Label}";
    }
}