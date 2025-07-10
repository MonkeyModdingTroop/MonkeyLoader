using EnumerableToolkit;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Zio;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Contains the base metadata and references for a mod.
    /// </summary>
    public abstract partial class Mod : IConfigOwner, IShutdown, ILoadedNuGetPackage, IComparable<Mod>,
        INestedIdentifiableOwner<ConfigSection>, INestedIdentifiableOwner<IDefiningConfigKey>,
        IIdentifiableOwner<Mod, IMonkey>, IIdentifiableOwner<Mod, IEarlyMonkey>, IAuthorable

    {
        /// <summary>
        /// The file extension for mods' assemblies.
        /// </summary>
        protected const string AssemblyExtension = ".dll";

        /// <summary>
        /// Stores the paths to the mod's assemblies inside the mod's <see cref="FileSystem">FileSystem</see>.
        /// </summary>
        protected readonly SortedSet<UPath> assemblyPaths = new(UPath.DefaultComparerIgnoreCase);

        /// <summary>
        /// Stores the authors of this mod.
        /// </summary>
        protected readonly HashSet<string> authors = new(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Stores the paths to the mod's content files inside the mod's <see cref="FileSystem">FileSystem</see>.
        /// </summary>
        protected readonly SortedSet<UPath> contentPaths = [];

        /// <summary>
        /// Stores the dependencies of this mod.
        /// </summary>
        protected readonly Dictionary<string, DependencyReference> dependencies = new(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Stores the pre-patchers of this mod.
        /// </summary>
        protected readonly SortedSet<IEarlyMonkey> earlyMonkeys = new(Monkey.AscendingComparer);

        /// <summary>
        /// Stores the patchers of this mod.
        /// </summary>
        protected readonly SortedSet<IMonkey> monkeys = new(Monkey.AscendingComparer);

        /// <summary>
        /// Stores the tags of this mod.
        /// </summary>
        protected readonly HashSet<string> tags = new(StringComparer.InvariantCultureIgnoreCase);

        private readonly Lazy<Config> _config;

        private readonly Lazy<string> _configPath;

        private readonly Lazy<Logger> _logger;

        private readonly Lazy<MonkeyTogglesConfigSection> _monkeyToggles;
        private bool _allDependenciesLoaded = false;

        /// <summary>
        /// Gets an <see cref="IComparer{T}"/> that keeps <see cref="Mod"/>s sorted in topological order.
        /// </summary>
        public static IComparer<Mod> AscendingComparer { get; } = new ModComparer(true);

        /// <summary>
        /// Gets an <see cref="IComparer{T}"/> that keeps <see cref="Mod"/>s sorted in reverse topological order.
        /// </summary>
        public static IComparer<Mod> DescendingComparer { get; } = new ModComparer(false);

        /// <inheritdoc/>
        public bool AllDependenciesLoaded
        {
            get
            {
                // Todo: remove this once dependencies have been figured out
                return true;

                if (!_allDependenciesLoaded)
                    _allDependenciesLoaded = dependencies.Values.All(dep => dep.AllDependenciesLoaded);

                return _allDependenciesLoaded;
            }
        }

        /// <summary>
        /// Gets the paths to the mod's assemblies inside the mod's <see cref="FileSystem">FileSystem</see>.
        /// </summary>
        public IEnumerable<UPath> AssemblyPaths => assemblyPaths.AsSafeEnumerable();

        /// <summary>
        /// Gets the names of the authors of this mod.
        /// </summary>
        public IEnumerable<string> Authors => authors.AsSafeEnumerable();

        /// <summary>
        /// Gets the config that this mod's (pre-)patcher(s) can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config => _config.Value;

        /// <summary>
        /// Gets the path where this mod's config file should be.
        /// </summary>
        public string ConfigPath => _configPath.Value;

        /// <summary>
        /// Gets the paths to the mod's content files inside the mod's <see cref="FileSystem">FileSystem</see>.
        /// </summary>
        public IEnumerable<UPath> ContentPaths => contentPaths.AsSafeEnumerable();

        /// <summary>
        /// Gets the dependencies of this mod.
        /// </summary>
        public IEnumerable<DependencyReference> Dependencies => dependencies.Values.AsSafeEnumerable();

        /// <summary>
        /// Gets the description of this mod.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the available <see cref="IEarlyMonkey"/>s of this mod, with the highest impact ones coming first.
        /// </summary>
        public IEnumerable<IEarlyMonkey> EarlyMonkeys => earlyMonkeys.AsSafeEnumerable();

        /// <summary>
        /// Gets the readonly file system of this mod's file.
        /// </summary>
        public abstract IFileSystem FileSystem { get; }

        string IIdentifiable.FullId => Id;

        /// <summary>
        /// Gets whether this mod has any <see cref="Monkeys">monkeys</see>.
        /// </summary>
        public bool HasPatchers => monkeys.Count > 0;

        /// <summary>
        /// Gets whether this mod has any <see cref="EarlyMonkeys">early monkeys</see>.
        /// </summary>
        public bool HasPrePatchers => earlyMonkeys.Count > 0;

        /// <summary>
        /// Gets the path to the mod's icon inside the mod's <see cref="FileSystem">FileSystem</see>.<br/>
        /// <c>null</c> if it wasn't given or doesn't exist.
        /// </summary>
        public abstract UPath? IconPath { get; }

        /// <summary>
        /// Gets the Url to the mod's icon on the web.<br/>
        /// <c>null</c> if it wasn't given or was invalid.
        /// </summary>
        public abstract Uri? IconUrl { get; }

        /// <summary>
        /// Gets the unique identifier of this mod.
        /// </summary>
        public string Id => Identity.Id;

        /// <summary>
        /// Gets the identity of this mod.
        /// </summary>
        public abstract PackageIdentity Identity { get; }

        /// <summary>
        /// Gets whether this mod is a game pack.
        /// </summary>
        public bool IsGamePack { get; }

        IEnumerable<IMonkey> IIdentifiableOwner<IMonkey>.Items => monkeys.Concat(earlyMonkeys);

        IEnumerable<IEarlyMonkey> IIdentifiableOwner<IEarlyMonkey>.Items => EarlyMonkeys;

        IEnumerable<IDefiningConfigKey> INestedIdentifiableOwner<IDefiningConfigKey>.Items
            => Config.Sections.SelectMany(section => section.Keys);

        IEnumerable<Config> IIdentifiableOwner<Config>.Items => Config.Yield();

        IEnumerable<ConfigSection> INestedIdentifiableOwner<ConfigSection>.Items => Config.Sections;

        /// <summary>
        /// Gets whether this mod's <see cref="LoadEarlyMonkeys">LoadEarlyMonkeys</see>() failed when it was called.
        /// </summary>
        public bool LoadEarlyMonkeysFailed { get; private set; } = false;

        /// <summary>
        /// Gets whether this mod's <see cref="LoadEarlyMonkeys">LoadEarlyMonkeys</see>() method has been called.
        /// </summary>
        public bool LoadedEarlyMonkeys { get; private set; } = false;

        /// <summary>
        /// Gets whether this mod's <see cref="LoadMonkeys">LoadMonkeys</see>() method has been called.
        /// </summary>
        public bool LoadedMonkeys { get; private set; } = false;

        /// <summary>
        /// Gets the <see cref="MonkeyLoader"/> instance that loaded this mod.
        /// </summary>
        public MonkeyLoader Loader { get; }

        /// <summary>
        /// Gets whether this mod's <see cref="LoadEarlyMonkeys">LoadEarlyMonkeys</see>() or <see cref="LoadMonkeys">LoadMonkeys</see>() failed when they were called.
        /// </summary>
        public bool LoadFailed => LoadEarlyMonkeysFailed || LoadMonkeysFailed;

        /// <summary>
        /// Gets whether this mod's <see cref="LoadMonkeys">LoadMonkeys</see>() failed when it was called.
        /// </summary>
        public bool LoadMonkeysFailed { get; private set; } = false;

        /// <summary>
        /// Gets the absolute path to this mod's file. May be <c>null</c> if the mod only exists in memory.
        /// </summary>
        public string? Location { get; }

        /// <summary>
        /// Gets the logger to be used by this mod.
        /// </summary>
        /// <remarks>
        /// They all share the <see cref="Loader">Loader's</see> <see cref="LoggingController"/>.
        /// </remarks>
        public Logger Logger => _logger.Value;

        /// <summary>
        /// Gets the available <see cref="IMonkey"/>s of this mod, with the highest impact ones coming first.
        /// </summary>
        public IEnumerable<IMonkey> Monkeys => monkeys.AsSafeEnumerable();

        /// <summary>
        /// Gets the toggles for this mod's monkeys that support disabling.
        /// </summary>
        public MonkeyTogglesConfigSection MonkeyToggles => _monkeyToggles.Value;

        /// <summary>
        /// Gets the Url to this mod's project website.<br/>
        /// <c>null</c> if it wasn't given or was invalid.
        /// </summary>
        public abstract Uri? ProjectUrl { get; }

        /// <summary>
        /// Gets the release notes for this mod's version.
        /// </summary>
        public abstract string? ReleaseNotes { get; }

        /// <summary>
        /// Gets whether this <see cref="Mod"/>'s <see cref="Shutdown"/> method failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; private set; } = false;

        /// <summary>
        /// Gets whether this <see cref="Mod"/>'s <see cref="Shutdown"/> method has been called.
        /// </summary>
        public bool ShutdownRan { get; private set; } = false;

        /// <summary>
        /// Gets whether this type of mod supports hot reloading.
        /// </summary>
        public abstract bool SupportsHotReload { get; }

        /// <summary>
        /// Gets the tags of this mod.
        /// </summary>
        public IEnumerable<string> Tags => tags.AsSafeEnumerable();

        /// <summary>
        /// Gets the framework targeted by this mod.
        /// </summary>
        public abstract NuGetFramework TargetFramework { get; }

        /// <summary>
        /// Gets the nice identifier of this mod.
        /// </summary>
        public virtual string Title => Id;

        /// <summary>
        /// Gets this mod's version.
        /// </summary>
        public NuGetVersion Version => Identity.Version;

        /// <summary>
        /// Creates a new mod instance with the given details.
        /// </summary>
        /// <param name="loader">The loader instance that loaded this mod.</param>
        /// <param name="location">The absolute path to this mod's file. May be <c>null</c> if the mod only exists in memory.</param>
        /// <param name="isGamePack">Whether this mod is a game pack.</param>
        protected Mod(MonkeyLoader loader, string? location, bool isGamePack)
        {
            Loader = loader;
            Location = location;
            IsGamePack = isGamePack;

            // Lazy, because the properties used to create them are only assigned after this constructor.
            _logger = new(() => new Logger(loader.Logger, Title));
            _configPath = new(() => Path.Combine(Loader.Locations.Configs, $"{Id}.json"));
            _config = new(() => new Config(this));
            _monkeyToggles = new(() => Config.LoadSection(new MonkeyTogglesConfigSection(this)));
        }

        /// <summary>
        /// Compares this mod with another and returns a value indicating whether
        /// one is dependent on the other, independent, or the other dependent on this.
        /// </summary>
        /// <param name="other">A mod to compare with this instance.</param>
        /// <returns>
        /// A signed integer that indicates the dependency relation:<br/>
        /// <i>Less than zero:</i> this is a dependency of <paramref name="other"/>.<br/>
        /// <i>Zero:</i> this and <paramref name="other"/> are independent.<br/>
        /// <i>Greater than zero:</i> this is dependent on <paramref name="other"/>.
        /// </returns>
        public int CompareTo(Mod other) => AscendingComparer.Compare(this, other);

        /// <inheritdoc/>
        public bool DependsOn(string otherId)
            => otherId == Identity.Id || Dependencies.Any(reference => reference.TransitivelyReferences(otherId));

        /// <inheritdoc/>
        public bool DependsOn(ILoadedNuGetPackage otherPackage) => DependsOn(otherPackage.Identity.Id);

        /// <summary>
        /// Efficiently  determines whether the given
        /// <paramref name="name"/> is listed as an author for this mod.
        /// </summary>
        /// <param name="name">The name to check for.</param>
        /// <returns><c>true</c> if the given <paramref name="name"/> is listed as an author for this mod.</returns>
        public bool HasAuthor(string name) => authors.Contains(name);

        /// <summary>
        /// Efficiently checks, whether a given tag is listed for this mod.
        /// </summary>
        /// <param name="tag">The tag to check for.</param>
        /// <returns><c>true</c> if the given tag is listed for this mod; otherwise, <c>false</c>.</returns>
        public bool HasTag(string tag) => tags.Contains(tag);

        /// <summary>
        /// Lets this mod cleanup and shutdown.<br/>
        /// Must only be called once.
        /// </summary>
        /// <inheritdoc/>
        public bool Shutdown(bool applicationExiting)
        {
            if (ShutdownRan)
                throw new InvalidOperationException("A mod's Shutdown() method must only be called once!");

            ShutdownRan = true;

            Logger.Debug(() => "Running OnShutdown!");
            OnShuttingDown(applicationExiting);

            if (!applicationExiting)
                Loader.EventManager.UnregisterMod(this);

            try
            {
                if (!OnShutdown(applicationExiting))
                {
                    ShutdownFailed = true;
                    Logger.Warn(() => "OnShutdown failed!");
                }
            }
            catch (Exception ex)
            {
                ShutdownFailed = true;
                Logger.Error(ex.LogFormat("OnShutdown threw an Exception:"));
            }

            OnShutdownDone(applicationExiting);
            Logger.Debug(() => "OnShutdown done!");

            return !ShutdownFailed;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Identity} ({(IsGamePack ? "Game Pack" : "Regular")})";

        /// <inheritdoc/>
        public bool TryResolveDependencies()
            => dependencies.Values.Select(dep => dep.TryResolve()).AllTrue();

        internal bool LoadEarlyMonkeys()
        {
            if (LoadedEarlyMonkeys)
                throw new InvalidOperationException("A mod's LoadEarlyMonkeys() method must only be called once!");

            LoadedEarlyMonkeys = true;

            try
            {
                if (!OnLoadEarlyMonkeys())
                {
                    LoadEarlyMonkeysFailed = true;
                    Logger.Warn(() => "OnLoadEarlyMonkey failed!");
                }
            }
            catch (Exception ex)
            {
                LoadEarlyMonkeysFailed = true;
                Logger.Error(ex.LogFormat("A mod's OnLoadEarlyMonkeys() method threw an Exception:"));
            }

            return !LoadEarlyMonkeysFailed;
        }

        internal bool LoadMonkeys()
        {
            if (LoadedMonkeys)
                throw new InvalidOperationException("A mod's LoadMonkeys() method must only be called once!");

            LoadedMonkeys = true;

            try
            {
                if (!OnLoadMonkeys())
                {
                    LoadMonkeysFailed = true;
                    Logger.Warn(() => "OnLoadEarlyMonkey failed!");
                }
            }
            catch (Exception ex)
            {
                LoadMonkeysFailed = true;
                Logger.Error(ex.LogFormat("A mod's OnLoadMonkeys() method threw an Exception:"));
            }

            return !LoadMonkeysFailed;
        }

        /// <summary>
        /// Loads the mod's <see cref="EarlyMonkeys">early monkeys</see>.
        /// </summary>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        protected abstract bool OnLoadEarlyMonkeys();

        /// <summary>
        /// Loads the mod's <see cref="Monkeys">monkeys</see>.
        /// </summary>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        protected abstract bool OnLoadMonkeys();

        /// <summary>
        /// Lets this mod cleanup and shutdown.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> <see cref="Config.Save">Saves</see> this mod's <see cref="Config">Config</see>.
        /// </remarks>
        /// <param name="applicationExiting">Whether the shutdown was caused by the application exiting.</param>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        protected virtual bool OnShutdown(bool applicationExiting)
        {
            Config.Save();

            return true;
        }

        private void OnShutdownDone(bool applicationExiting)
        {
            try
            {
                ShutdownDone?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex.LogFormat($"Some {nameof(ShutdownDone)} event subscriber(s) threw an exception:"));
            }
        }

        private void OnShuttingDown(bool applicationExiting)
        {
            try
            {
                ShuttingDown?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex.LogFormat($"Some {nameof(ShuttingDown)} event subscriber(s) threw an exception:"));
            }
        }

        /// <inheritdoc/>
        public event ShutdownHandler? ShutdownDone;

        /// <inheritdoc/>
        public event ShutdownHandler? ShuttingDown;

        private sealed class ModComparer : IComparer<Mod>
        {
            private readonly int _factor;

            public ModComparer(bool ascending = true)
            {
                _factor = ascending ? 1 : -1;
            }

            /// <inheritdoc/>
            public int Compare(Mod x, Mod y)
            {
                // Game Packs always first
                if (x.IsGamePack ^ y.IsGamePack)
                    return _factor * (x.IsGamePack ? -1 : 1);

                var xDependsOnY = x.DependsOn(y);
                var yDependsOnX = y.DependsOn(x);

                if (xDependsOnY ^ yDependsOnX)
                    return _factor * (xDependsOnY ? 1 : -1);

                // Fall back to alphabetical order to prevent false equivalence
                return _factor * x.Id.CompareTo(y.Id);
            }
        }
    }
}