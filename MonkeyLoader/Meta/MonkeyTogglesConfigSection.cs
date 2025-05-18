using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Specifies the toggles for the Monkeys of a <see cref="Meta.Mod"/> which support disabling.
    /// </summary>
    public sealed class MonkeyTogglesConfigSection : ExpandoConfigSection
    {
        private readonly Dictionary<IMonkey, IDefiningConfigKey<bool>> _togglesByMonkey = [];

        /// <inheritdoc/>
        public override string Description => "Contains toggles for the Monkeys of a mod which support disabling.";

        /// <inheritdoc/>
        public override string Id => "MonkeyToggles";

        /// <inheritdoc/>
        public override bool InternalAccessOnly => true;

        /// <summary>
        /// Gets the <see cref="Meta.Mod"/> that these toggles are for.
        /// </summary>
        public Mod Mod { get; }

        /// <inheritdoc/>
        public override Version Version => Mod.Version.Version;

        internal MonkeyTogglesConfigSection(Mod mod)
        {
            Mod = mod;
        }

        /// <summary>
        /// Creates a template key for the toggle of the given (early) monkey.
        /// </summary>
        /// <param name="monkey">The (early) monkey to create a template key for.</param>
        /// <returns>The template key for the toggle of the (early) monkey.</returns>
        public static ITypedConfigKey<bool> GetTemplateKey(IMonkey monkey)
            => new ConfigKey<bool>(monkey.Id);

        /// <summary>
        /// Gets or creates the toggle config item for the given (early) monkey.
        /// </summary>
        /// <remarks>
        /// The default for toggles created with this method is always <c>true</c>.
        /// </remarks>
        /// <inheritdoc cref="GetToggle(IMonkey, Func{bool})"/>
        public IDefiningConfigKey<bool> GetToggle(IMonkey monkey)
            => GetToggle(monkey, static () => true);

        /// <summary>
        /// Gets or creates the toggle config item for the given (early) monkey,
        /// while passing along the given method to compute its default state.
        /// </summary>
        /// <param name="monkey">
        /// The (early) monkey to get the key for. Must belong to the same
        /// <see cref="Meta.Mod"/> and support <see cref="IMonkey.CanBeDisabled">being disabled</see>.
        /// </param>
        /// <param name="computeDefault">The function that computes the default state for this toggle.</param>
        /// <returns>The toggle config item for the given (early) monkey.</returns>
        /// <exception cref="ArgumentNullException">When the <paramref name="monkey"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// When the <paramref name="monkey"/> is from another <see cref="Meta.Mod"/>
        /// or doesn't support <see cref="IMonkey.CanBeDisabled">being disabled</see>.
        /// </exception>
        public IDefiningConfigKey<bool> GetToggle(IMonkey monkey, Func<bool> computeDefault)
        {
            if (monkey is null)
                throw new ArgumentNullException(nameof(monkey));

            if (computeDefault is null)
                throw new ArgumentNullException(nameof(computeDefault));

            if (monkey.Mod != Mod || !monkey.CanBeDisabled)
                throw new ArgumentException("Monkey doesn't belong to this section's mod or can't be disabled!");

            if (!_togglesByMonkey.TryGetValue(monkey, out var toggleKey))
            {
                toggleKey = GetOrCreateDefiningKey(GetTemplateKey(monkey),
                    $"Whether the {(monkey is IEarlyMonkey ? "Early Monkey" : "Monkey")} {monkey.Name} should be active.", computeDefault, true);

                _togglesByMonkey.Add(monkey, toggleKey);
            }

            return toggleKey;
        }
    }
}