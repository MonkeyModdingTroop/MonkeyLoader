﻿using MonkeyLoader.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.NuGet
{
    /// <summary>
    /// Handles accessing NuGet feeds and loading dependencies.
    /// </summary>
    public sealed class NuGetManager
    {
        private readonly Dictionary<string, ILoadedNuGetPackage> _loadedPackages = new(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Gets the config used by the manager.
        /// </summary>
        public NuGetConfigSection Config { get; }

        /// <summary>
        /// Gets the loader this NuGet manager works for.
        /// </summary>
        public MonkeyLoader Loader { get; }

        /// <summary>
        /// Gets the logger used by the manager.
        /// </summary>
        public Logger Logger { get; }

        /// <summary>
        /// Creates a new NuGet manager instance that works for the given loader.<br/>
        /// Requires <see cref="MonkeyLoader.Logger"/> and <see cref="MonkeyLoader.Config"/> to be set.
        /// </summary>
        /// <param name="loader">The loader this NuGet manager works for.</param>
        internal NuGetManager(MonkeyLoader loader)
        {
            Loader = loader;
            Logger = new Logger(loader.Logger, "NuGet");
            Config = loader.Config.LoadSection<NuGetConfigSection>();

            Logger.Info(() => $"Detected Runtime Target NuGet Framework: {NuGetHelper.Framework} ({NuGetHelper.Framework.GetShortFolderName()})");
            Logger.Debug(() => $"Compatible NuGet Frameworks:{Environment.NewLine}" +
                $"    - {string.Join($"{Environment.NewLine}    - ", NuGetHelper.CompatibleFrameworks.Select(fw => $"{fw} ({fw.GetShortFolderName()})"))}");
        }

        public void Add(ILoadedNuGetPackage package)
        {
            if (_loadedPackages.ContainsKey(package.Identity.Id))
            {
                Logger.Warn(() => $"Already added loaded package [{package.Identity}]");
                return;
            }

            _loadedPackages.Add(package.Identity.Id, package);
            Logger.Trace(() => $"Added loaded package [{package.Identity}]");
        }

        public void AddAll(IEnumerable<ILoadedNuGetPackage> packages)
        {
            foreach (var package in packages)
                Add(package);
        }

        public ILoadedNuGetPackage Resolve(string id)
        {
            if (!TryResolve(id, out var package))
                throw new KeyNotFoundException($"No package with id [{id}] could be found!");

            return package;
        }

        public bool TryResolve(string id, [NotNullWhen(true)] out ILoadedNuGetPackage? package)
        {
            var success = _loadedPackages.TryGetValue(id, out package);
            Logger.Trace(() => $"Attempted to resolve package with id [{id}] - {(success ? "success" : "failed")}");

            return success;
        }
    }
}