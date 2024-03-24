using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Interface to let <see cref="Mod">mods</see> and the <see cref="MonkeyLoader">loader</see> own <see cref="Config"/>s.
    /// </summary>
    public interface IConfigOwner
    {
        /// <summary>
        /// Gets the path where this owner's config file should be.
        /// </summary>
        public string ConfigPath { get; }

        /// <summary>
        /// Gets the unique identifier of this owner.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the <see cref="MonkeyLoader"/> instance that loaded this owner.
        /// </summary>
        public MonkeyLoader Loader { get; }

        /// <summary>
        /// Gets the logger to be used by this owner.
        /// </summary>
        /// <remarks>
        /// All loggers share the <see cref="Loader">Loader</see>'s <see cref="MonkeyLoader.LoggingController">LoggingController</see>.
        /// </remarks>
        public Logger Logger { get; }
    }
}