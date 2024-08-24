using EnumerableToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Implements a priority component for <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    public sealed class ConfigKeyPriority : IConfigKeyPriority
    {
        /// <inheritdoc/>
        public int Priority { get; }

        /// <summary>
        /// Creates a new priority component with the given priority value.<br/>
        /// Without this component, <see cref="IDefiningConfigKey"/>s have a priority of 0.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="HarmonyLib.Priority"/> values as a base.
        /// Higher comes first.
        /// </remarks>
        /// <param name="priority">The priority value to use. Use the <see cref="HarmonyLib.Priority"/> values as a base. Higher comes first.</param>
        public ConfigKeyPriority(int priority)
        {
            Priority = priority;
        }

        /// <inheritdoc/>
        public void Initialize(IDefiningConfigKey entity)
        { }
    }

    /// <summary>
    /// Defines the interface for priority components for <see cref="IDefiningConfigKey"/>s.
    /// </summary>
    /// <remarks>
    /// This component is used to set the <see cref="IPrioritizable.Priority">Priority</see> property for <see cref="IDefiningConfigKey"/>s.
    /// </remarks>
    public interface IConfigKeyPriority : IConfigKeyComponent<IDefiningConfigKey>, IPrioritizable
    { }
}