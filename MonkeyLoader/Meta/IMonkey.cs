using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Defines the interface for all <see cref="EarlyMonkey{TMonkey}">early monkeys</see>.
    /// </summary>
    public interface IEarlyMonkey : IMonkey
    {
        /// <summary>
        /// Gets the pre-patch targets that were successfully applied.<br/>
        /// This may be a larger set than <see cref="PrePatchTargets">PrePatchTargets</see>
        /// if this pre-patcher <see cref="TargetsAllAssemblies">targets all assemblies</see>.
        /// </summary>
        public IEnumerable<PrePatchTarget> ExecutedPatches { get; }

        /// <summary>
        /// Gets the names of the assemblies and types therein which this pre-patcher targets.
        /// </summary>
        public IEnumerable<PrePatchTarget> PrePatchTargets { get; }

        /// <summary>
        /// Gets whether this pre-patcher targets all available assemblies.
        /// </summary>
        public bool TargetsAllAssemblies { get; }
    }

    /// <summary>
    /// Defines the interface for all (<see cref="EarlyMonkey{TMonkey}">early</see>)
    /// <see cref="Monkey{TMonkey}">monkeys</see>.
    /// </summary>
    public interface IMonkey : IRun, IShutdown, IComparable<IMonkey>, INestedIdentifiable<Mod>
    {
        /// <summary>
        /// Gets the name of the assembly this monkey is defined in.
        /// </summary>
        public AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets whether this monkey can be disabled, that is, whether it's
        /// permitted to set <see cref="Enabled">Enabled</see> to <c>false</c>,
        /// and there is an <see cref="EnabledToggle">EnabledToggle</see>.
        /// </summary>
        /// <value>
        /// <c>true</c> if this monkey respects the <see cref="Mod.MonkeyToggles"/> config.
        /// </value>
        [MemberNotNullWhen(true, nameof(EnabledToggle))]
        public bool CanBeDisabled { get; }

        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this monkey can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets or sets whether this monkey should currently be active.
        /// </summary>
        /// <remarks>
        /// Can only be set to <c>false</c> if the monkey
        /// supports <see cref="CanBeDisabled">being disabled</see>.
        /// </remarks>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the this monkey's <see cref="MonkeyTogglesConfigSection.GetToggle">toggle</see>
        /// if it <see cref="CanBeDisabled">can be disabled</see>.
        /// </summary>
        /// <value>The toggle config item if this monkey <see cref="CanBeDisabled">can be disabled</see>; otherwise, <c>null</c>.</value>
        public IDefiningConfigKey<bool>? EnabledToggle { get; }

        /// <summary>
        /// Gets the impacts this (pre-)patcher has on certain features ordered by descending impact.
        /// </summary>
        public IEnumerable<IFeaturePatch> FeaturePatches { get; }

        /// <summary>
        /// Gets the <see cref="HarmonyLib.Harmony">Harmony</see> instance to be used by this patcher.
        /// </summary>
        public Harmony Harmony { get; }

        /// <summary>
        /// Gets the <see cref="Logging.Logger"/> that this monkey can use to log messages to game-specific channels.
        /// </summary>
        public Logger Logger { get; }

        /// <summary>
        /// Gets the mod that this monkey is a part of.
        /// </summary>
        public Mod Mod { get; }

        /// <summary>
        /// Gets the display name of this monkey.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the runtime type of this monkey.
        /// </summary>
        public Type Type { get; }
    }
}