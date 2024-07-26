using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents a section of a <see cref="Config"/> for any <see cref="IConfigOwner"/>,
    /// of which there can only ever be once instance.<br/>
    /// This is primarily useful for <c>public</c> config sections which others mods should be able to access.
    /// </summary>
    /// <typeparam name="TConfigSection">The type of the actual config section.</typeparam>
    /// <inheritdoc/>
    public abstract class SingletonConfigSection<TConfigSection> : ConfigSection
        where TConfigSection : SingletonConfigSection<TConfigSection>
    {
        /// <summary>
        /// Gets this singleton <see cref="ConfigSection"/>'s instance.
        /// </summary>
        public static TConfigSection Instance { get; private set; } = null!;

        /// <summary>
        /// Creates an instance of this config section once.
        /// </summary>
        /// <exception cref="InvalidOperationException">When TConfigSection isn't the concrete type, or there is already an <see cref="Instance">Instance</see> of this config section.</exception>
        public SingletonConfigSection()
        {
            if (GetType() != typeof(TConfigSection))
                throw new InvalidOperationException("TConfigSection must be the concrete Type being instantiated!");

            if (Instance is not null)
                throw new InvalidOperationException();

            Instance = (TConfigSection)this;
        }
    }
}