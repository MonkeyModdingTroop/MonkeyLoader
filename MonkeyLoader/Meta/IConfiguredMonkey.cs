using MonkeyLoader.Configuration;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the non-generic interface for all sorts of
    /// <see cref="ConfiguredMonkey{TMonkey, TConfigSection}">configured monkeys</see>.
    /// </summary>
    public interface IConfiguredMonkey : IMonkey
    {
        /// <summary>
        /// Gets the loaded config section for this monkey after it has been run.
        /// </summary>
        public ConfigSection ConfigSection { get; }
    }

    /// <summary>
    /// Defines the interface for all sorts of
    /// <see cref="ConfiguredMonkey{TMonkey, TConfigSection}">configured monkeys</see>.
    /// </summary>
    /// <typeparam name="TConfigSection">The type of the config section loaded by this monkey.</typeparam>
    public interface IConfiguredMonkey<TConfigSection> : IConfiguredMonkey
        where TConfigSection : ConfigSection
    {
        /// <summary>
        /// Gets the loaded config section for this monkey after it has been run.
        /// </summary>
        public new TConfigSection ConfigSection { get; }
    }
}