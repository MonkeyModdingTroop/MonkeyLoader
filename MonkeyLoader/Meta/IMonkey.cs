﻿using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Interface for <see cref="EarlyMonkey{TMonkey}"/>s.
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
    /// The interface for any monkey.
    /// </summary>
    public interface IMonkey : IRun, IShutdown, IComparable<IMonkey>
    {
        /// <summary>
        /// Gets the name of the assembly this monkey is defined in.
        /// </summary>
        public AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this monkey can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config { get; }

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
        /// Gets this monkey's name.
        /// </summary>
        public string Name { get; }
    }
}