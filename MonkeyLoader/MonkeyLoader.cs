using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Events;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MonkeyLoader
{
    /// <summary>
    /// The delegate that gets called when a mod is added to a <see cref="MonkeyLoader"/>.
    /// </summary>
    /// <param name="loader">The loader responsible for the mod.</param>
    /// <param name="mod">The mod that was added.</param>
    public delegate void ModChangedEventHandler(MonkeyLoader loader, Mod mod);

    /// <summary>
    /// The delegate that gets called when mods are run or shut down by a <see cref="MonkeyLoader"/>.
    /// </summary>
    /// <param name="loader">The loader responsible for the mods.</param>
    /// <param name="mods">The mods that were run or shut down.</param>
    public delegate void ModsChangedEventHandler(MonkeyLoader loader, IEnumerable<Mod> mods);

    /// <summary>
    /// The root of all mod loading.
    /// </summary>
    public sealed class MonkeyLoader : IConfigOwner, IShutdown, IDisplayable, IIdentifiableCollection<Mod>,
        INestedIdentifiableCollection<IMonkey>, INestedIdentifiableCollection<IEarlyMonkey>,
        INestedIdentifiableCollection<Config>, INestedIdentifiableCollection<ConfigSection>, INestedIdentifiableCollection<IDefiningConfigKey>
    {
        /// <summary>
        /// The default path for the MonkeyLoader config file.
        /// </summary>
        public const string DefaultConfigPath = "MonkeyLoader/MonkeyLoader.json";

        /// <summary>
        /// All the currently loaded and still active mods of this loader, kept in topological order.
        /// </summary>
        private readonly SortedSet<Mod> _allMods = new(Mod.AscendingComparer);

        /// <summary>
        /// Gets the <see cref="StringComparison"/> mode used by the current
        /// <see cref="RuntimeInformation.IsOSPlatform(OSPlatform)">OS Platform</see>'s filesystem.
        /// </summary>
        public static StringComparison FilesystemComparison { get; }
            = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        /// <summary>
        /// Gets the <see cref="StringComparer"/> used by the current
        /// <see cref="RuntimeInformation.IsOSPlatform(OSPlatform)">OS Platform</see>'s filesystem.
        /// </summary>
        public static StringComparer FilesystemComparer { get; }
            = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

        private ExecutionPhase _phase;

        /// <summary>
        /// Gets the path pointing of the directory containing the game's assemblies.
        /// </summary>
        public static string GameAssemblyPath { get; }

        /// <summary>
        /// Gets the name of the game (its executable).
        /// </summary>
        public static string GameName { get; }

        /// <summary>
        /// Gets the path pointing to the directory containing runtime assemblies.
        /// </summary>
        public static string RuntimeAssemblyPath { get; }

        /// <summary>
        /// Gets the config that this loader uses to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets the path where the loader's config file should be.
        /// </summary>
        public string ConfigPath { get; }

        /// <inheritdoc/>
        public string Description => "Handles all the mod loading.";

        string IIdentifiable.FullId => Id;

        /// <summary>
        /// Gets all loaded game pack <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<Mod> GamePacks => _allMods.Where(mod => mod.IsGamePack);

        /// <inheritdoc/>
        bool IDisplayable.HasDescription => true;

        /// <summary>
        /// Gets this loader's id.
        /// </summary>
        public string Id { get; }

        IEnumerable<Mod> IIdentifiableCollection<Mod>.Items => Mods;

        IEnumerable<ConfigSection> INestedIdentifiableCollection<ConfigSection>.Items
            => Config.Sections.Concat(_allMods.SelectMany(mod => mod.Config.Sections));

        IEnumerable<IDefiningConfigKey> INestedIdentifiableCollection<IDefiningConfigKey>.Items
            => ((INestedIdentifiableCollection<ConfigSection>)this).Items.SelectMany(configSection => configSection.Keys);

        IEnumerable<IEarlyMonkey> INestedIdentifiableCollection<IEarlyMonkey>.Items
            => _allMods.SelectMany(mod => mod.EarlyMonkeys);

        IEnumerable<IMonkey> INestedIdentifiableCollection<IMonkey>.Items
            => _allMods.SelectMany(mod => mod.Monkeys).Concat(_allMods.SelectMany(mod => mod.EarlyMonkeys));

        IEnumerable<Config> IIdentifiableOwner<Config>.Items => Config.Yield();

        IEnumerable<Config> INestedIdentifiableCollection<Config>.Items
            => Config.Yield().Concat(_allMods.Select(mod => mod.Config));

        /// <summary>
        /// Gets the json serializer used by this loader and any mods it loads.<br/>
        /// Will be populated with any converters picked up from game integration packs.
        /// </summary>
        public JsonSerializer JsonSerializer { get; }

        MonkeyLoader IConfigOwner.Loader => this;

        /// <summary>
        /// Gets the configuration for which paths will be searched for certain resources.
        /// </summary>
        public LocationConfigSection Locations { get; }

        /// <summary>
        /// Gets the logger that's used by the loader and "inherited" from by everything loaded by it.
        /// </summary>
        public Logger Logger { get; }

        /// <summary>
        /// Gets the configuration for whether and how this loader should write logs.
        /// </summary>
        public LoggingConfig Logging { get; }

        /// <summary>
        /// Gets <i>all</i> loaded <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<Mod> Mods => _allMods.AsSafeEnumerable();

        string IDisplayable.Name => Id;

        /// <summary>
        /// Gets the NuGet manager used by this loader.
        /// </summary>
        public NuGetManager NuGet { get; private set; }

        /// <summary>
        /// Gets this loader's current <see cref="ExecutionPhase"/>.
        /// </summary>
        public ExecutionPhase Phase
        {
            get => _phase;

            private set
            {
                if (_phase == value)
                    return;

                if (_phase > value)
                    throw new InvalidOperationException($"Attempted to regress from phase [{_phase}] to [{value}]!");

                Logger.Info(() => $"Advanced from phase [{_phase}] to [{value}]!");
                _phase = value;
            }
        }

        /// <summary>
        /// Gets all loaded regular <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<Mod> RegularMods => _allMods.Where(mod => !mod.IsGamePack);

        /// <summary>
        /// Gets whether this loaders's <see cref="Shutdown">Shutdown</see>() failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; private set; }

        /// <summary>
        /// Gets whether this loader's <see cref="Shutdown">Shutdown</see>() method has been called.
        /// </summary>
        public bool ShutdownRan => Phase >= ExecutionPhase.ShuttingDown;

        internal EventManager EventManager { get; }

        internal AssemblyPool GameAssemblyPool { get; }
        internal AssemblyPool PatcherAssemblyPool { get; }
        internal AssemblyPool RuntimeAssemblyPool { get; }

        public IAssemblyLoadStrategy AssemblyLoadStrategy { get; }

        public Assembly? ResolveAssemblyFromPoolsAndMods(System.Reflection.AssemblyName assemblyName)
        {
            var mlAssemblyName = new AssemblyName(assemblyName.FullName);

            if (PatcherAssemblyPool.TryResolveAssembly(mlAssemblyName, out var assembly))
                return assembly;

            if (GameAssemblyPool.TryResolveAssembly(mlAssemblyName, out assembly))
                return assembly;

            foreach (var mod in Mods)
            {
                if (mod.TryResolveAssembly(mlAssemblyName, out assembly))
                    return assembly;
            }

            return null;
        }

        static MonkeyLoader()
        {
            // AppContext.BaseDirectory doesn't work for Unity
            // and we need the executable name for Unity too
            var executablePath = Environment.GetCommandLineArgs()[0];

            // Assume Unity structure
            var gameName = Path.GetFileNameWithoutExtension(executablePath);
            var gameAssemblyPath = Path.Combine(Path.GetDirectoryName(executablePath), $"{GameName}_Data", "Managed");

            if (!Directory.Exists(gameAssemblyPath))
            {
                // If Unity directory doesn't exist, assume plain .NET application
                DirectoryInfo executablePathInfo = new(executablePath);

                gameName = executablePathInfo.Parent.Name;
                gameAssemblyPath = executablePathInfo.Parent.FullName;
            }

            GameName = gameName;
            GameAssemblyPath = gameAssemblyPath;

            RuntimeAssemblyPath = RuntimeEnvironment.GetRuntimeDirectory();

            try
            {
                // Try applying the patches of the Mod Loader.
                // They're all written to be "safe", but better be sure no Exception escapes.
                var harmony = new Harmony("MonkeyLoader");
                harmony.PatchAll();
            }
            catch { }
        }

        /// <summary>
        /// Creates a new mod loader with the given configuration file.
        /// </summary>
        /// <param name="initialLoggingLevel">The initial <see cref="LoggingLevel"/> to pass to the <see cref="LoggingController"/> the loader is created with.</param>
        /// <param name="configPath">The path to the configuration file to use.</param>
        public MonkeyLoader(LoggingLevel initialLoggingLevel = LoggingLevel.Trace, string configPath = DefaultConfigPath)
            : this(new LoggingController(GetId(configPath)) { Level = initialLoggingLevel }, configPath)
        { }

        /// <summary>
        /// Creates a new mod loader with the given configuration file.
        /// </summary>
        /// <param name="loggingController">The logging controller that this loader should use or a default one when <c>null</c>.</param>
        /// <param name="configPath">The path to the configuration file to use.</param>
        public MonkeyLoader(LoggingController loggingController, string configPath = DefaultConfigPath)
        {
#if NET5_0_OR_GREATER
            AssemblyLoadStrategy = new AssemblyLoadContextLoadStrategy();
#endif

            ConfigPath = configPath;
            Id = GetId(configPath);

            Logger = new(loggingController);

            JsonSerializer = new();
            JsonSerializer.Converters.Add(new StringEnumConverter());

            Config = new Config(this);
            Locations = Config.LoadSection<LocationConfigSection>();
            Logging = Config.LoadSection<LoggingConfig>();
            Logging.Controller = loggingController;

            foreach (var modLocation in Locations.Mods)
            {
                modLocation.LoadMod += (mL, path) =>
                {
                    var stackTrace = Environment.StackTrace;
                    Logger.Info(() => $"Trying to hot-load mod from: {path}");
                    Logger.Info(() => stackTrace);
                    TryLoadAndRunMod(path, out _);
                };

                modLocation.UnloadMod += (mL, path) =>
                {
                    var stackTrace = Environment.StackTrace;
                    Logger.Info(() => $"Trying to unload mod from: {path}");
                    Logger.Info(() => stackTrace);

                    if (TryFindModByLocation(path, out var mod))
                        ShutdownMod(mod);
                };
            }

            // TODO: do this properly - scan all loaded assemblies?
            NuGet = new NuGetManager(this);
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("MonkeyLoader", new NuGetVersion(Assembly.GetExecutingAssembly().GetName().Version)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Newtonsoft.Json", new NuGetVersion(13, 0, 3)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("NuGet.Packaging", new NuGetVersion(6, 10, 0)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("NuGet.Protocol", new NuGetVersion(6, 10, 0)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Mono.Cecil", new NuGetVersion(0, 11, 5)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Harmony", new NuGetVersion(2, 3, 3)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Lib.Harmony", new NuGetVersion(2, 3, 3)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Lib.Harmony.Thin", new NuGetVersion(2, 3, 3)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Zio", new NuGetVersion(0, 18, 0)), NuGetHelper.Framework));

            RuntimeAssemblyPool = new AssemblyPool(this, "RuntimeAssemblyPool", () => Locations.PatchedAssemblies);
            RuntimeAssemblyPool.AddSearchDirectory(RuntimeAssemblyPath);

            GameAssemblyPool = new AssemblyPool(this, "GameAssemblyPool", () => Locations.PatchedAssemblies);
            GameAssemblyPool.AddSearchDirectory(GameAssemblyPath);
            GameAssemblyPool.AddFallbackPool(RuntimeAssemblyPool);

            PatcherAssemblyPool = new AssemblyPool(this, "PatcherAssemblyPool", () => Locations.PatchedAssemblies);
            PatcherAssemblyPool.AddFallbackPool(GameAssemblyPool);

            Phase = ExecutionPhase.Initialized;
            EventManager = new(this);
        }

        /// <summary>
        /// Instantiates and adds a <see cref="JsonConverter"/> instance of the given <typeparamref name="TConverter">converter type</typeparamref>
        /// to this loader's <see cref="JsonSerializer">JsonSerializer</see>.
        /// </summary>
        public void AddJsonConverter<TConverter>() where TConverter : JsonConverter, new()
            => AddJsonConverter(new TConverter());

        /// <summary>
        /// Adds the given <see cref="JsonConverter"/> instance to this loader's <see cref="JsonSerializer">JsonSerializer</see>.
        /// </summary>
        public void AddJsonConverter(JsonConverter jsonConverter) => JsonSerializer.Converters.Add(jsonConverter);

        /// <summary>
        /// Instantiates and adds a <see cref="JsonConverter"/> instance of the given <paramref name="converterType">converter type</paramref>
        /// to this loader's <see cref="JsonSerializer">JsonSerializer</see>.
        /// </summary>
        /// <param name="converterType">The <see cref="JsonConverter"/> derived type to instantiate.</param>
        public void AddJsonConverter(Type converterType)
            => AddJsonConverter((JsonConverter)Activator.CreateInstance(converterType));

        /// <summary>
        /// Searches the given <paramref name="assembly"/> for all instantiable types derived from <see cref="JsonConverter"/>,
        /// which are not decorated with the <see cref="IgnoreJsonConverterAttribute"/>.<br/>
        /// Instantiates adds an instance of them to this loader's <see cref="JsonSerializer">JsonSerializer</see>.
        /// </summary>
        /// <param name="assembly"></param>
        public void AddJsonConverters(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().ParameterlessInstantiable<JsonConverter>())
            {
                if (type.GetCustomAttribute<IgnoreJsonConverterAttribute>() is null)
                    AddJsonConverter(type);
            }
        }

        /// <summary>
        /// Adds a mod to be managed by this loader.
        /// </summary>
        /// <param name="mod">The mod to add.</param>
        /// <exception cref="InvalidOperationException">When the <paramref name="mod"/> mod is invalid.</exception>
        public void AddMod(Mod mod)
        {
            if (mod.Loader != this)
                throw new InvalidOperationException($"Attempted to add mod from another loader instance: {mod}");

            if (mod.ShutdownRan)
                throw new InvalidOperationException($"Attempted to add already shutdown mod: {mod}");

            if (_allMods.Add(mod))
            {
                Logger.Debug(() => $"Adding mod: {mod}");

                NuGet.Add(mod);

                ModAdded?.TryInvokeAll(this, mod);
            }
            else
            {
                Logger.Warn(() => $"Attempted to add already present mod: {mod}");
            }
        }

        /// <summary>
        /// Tries to create all <see cref="Locations">Locations</see> used by this loader.
        /// </summary>
        public void EnsureAllLocationsExist()
        {
            IEnumerable<string> locations = [Locations.Configs, Locations.GamePacks, Locations.Libs, Locations.PatchedAssemblies];
            var modLocations = Locations.Mods.Select(modLocation => modLocation.Path).ToArray();

            Logger.Info(() => $"Ensuring that all configured loading locations exist as directories:{Environment.NewLine}" +
                $"    {nameof(Locations.Configs)}: {Locations.Configs}{Environment.NewLine}" +
                $"    {nameof(Locations.GamePacks)}: {Locations.GamePacks}{Environment.NewLine}" +
                $"    {nameof(Locations.Libs)}: {Locations.Libs}{Environment.NewLine}" +
                $"    {nameof(Locations.PatchedAssemblies)}: {Locations.PatchedAssemblies}{Environment.NewLine}" +
                $"    {nameof(Locations.Mods)}:{Environment.NewLine}" +
                $"      - {string.Join(Environment.NewLine + "      - ", modLocations)}");

            foreach (var location in locations.Concat(modLocations))
            {
                try
                {
                    Directory.CreateDirectory(location);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.LogFormat($"Exception while trying to create directory: {location}"));
                }
            }

            foreach (var watchingModLocation in Locations.Mods.Where(modLocation => modLocation.SupportHotReload))
                watchingModLocation.ShouldWatcherBeActive = true;
        }

        /// <summary>
        /// Searches all of this loader's loaded <see cref="Mods">Mods</see> to find one with the given <see cref="Mod.Location">location</see>.
        /// </summary>
        /// <param name="location">The location to find a mod for.</param>
        /// <returns>The found mod.</returns>
        /// <exception cref="KeyNotFoundException">When no mod with the given location was found.</exception>
        public Mod FindModByLocation(string location)
        {
            if (!TryFindModByLocation(location, out var mod))
                throw new KeyNotFoundException(location);

            return mod;
        }

        /// <summary>
        /// Performs the full loading routine without customizations or interventions.
        /// </summary>
        public void FullLoad()
        {
            EnsureAllLocationsExist();

            LoadRuntimeAssemblyDefinitions();
            LoadGameAssemblyDefinitions();

            LoadAllLibraries();

            LoadAllGamePacks();
            LoadAllMods();

            LoadGamePackEarlyMonkeys();
            RunGamePackEarlyMonkeys();

            LoadModEarlyMonkeys();
            RunRegularEarlyMonkeys();

            LoadGameAssemblies();

            LoadGamePackMonkeys();
            RunGamePackMonkeys();

            LoadModMonkeys();
            RunRegularMonkeys();
        }

        /// <summary>
        /// Loads all game pack mods from the <see cref="LocationConfigSection">configured</see> <see cref="LocationConfigSection.GamePacks"> location</see>.
        /// </summary>
        /// <returns>All successfully loaded game pack mods.</returns>
        public IEnumerable<NuGetPackageMod> LoadAllGamePacks()
        {
            try
            {
                return Directory.EnumerateFiles(Locations.GamePacks, NuGetPackageMod.SearchPattern, SearchOption.TopDirectoryOnly)
                    .TrySelect<string, NuGetPackageMod>(TryLoadGamePack)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.LogFormat($"Exception while searching files at location {Locations.GamePacks}:"));
                return [];
            }
        }

        /// <summary>
        /// Loads all .nupkg files from the loader's <see cref="LocationConfigSection.Libs"/> folder.
        /// </summary>
        public void LoadAllLibraries()
        {
            try
            {
                Logger.Warn(() => "Loading libraries as NuGetPackageMods to make use of the logic - this should be changed.");

                // Abuse the NuGetPackageMod loading mechanisms to load the right libraries
                // It's already implemented there... but should be handled separately
                var loadedLibraries = Directory.EnumerateFiles(Locations.Libs, NuGetPackageMod.SearchPattern, SearchOption.TopDirectoryOnly)
                    .TrySelect<string, NuGetPackageMod>(TryLoadGamePack)
                    .ToArray();

                Logger.Info(() => "Loaded the following libraries:");
                Logger.Info(loadedLibraries);

                // Make sure the assemblies inside are loaded
                LoadMonkeys(loadedLibraries);

                // Remove the mod entries again
                ShutdownMods(loadedLibraries);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.LogFormat($"Exception while searching files at location {Locations.Libs} and loading libraries:"));
            }
        }

        /// <summary>
        /// Loads all mods from the <see cref="LocationConfigSection">configured</see> <see cref="ModLoadingLocation">locations</see>.
        /// </summary>
        /// <returns>All successfully loaded mods.</returns>
        public IEnumerable<NuGetPackageMod> LoadAllMods()
        {
            return Locations.Mods.SelectMany(location =>
            {
                try
                {
                    return location.Search();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.LogFormat($"Exception while searching files at location {location}:"));
                }

                return [];
            })
            .TrySelect<string, NuGetPackageMod>(TryLoadMod)
            .ToArray();
        }

        /// <summary>
        /// Loads every given <see cref="Mod"/>'s pre-patcher assemblies and <see cref="IEarlyMonkey"/>s.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="IEarlyMonkey"/>s to load.</param>
        public void LoadEarlyMonkeys(params Mod[] mods) => LoadEarlyMonkeys((IEnumerable<Mod>)mods);

        /// <summary>
        /// Loads every given <see cref="Mod"/>'s pre-patcher assemblies and <see cref="IEarlyMonkey"/>s.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="IEarlyMonkey"/>s to load.</param>
        public void LoadEarlyMonkeys(IEnumerable<Mod> mods)
        {
            Logger.Trace(() => "Loading the early monkeys of mods in this order:");
            Logger.Trace(mods);

            foreach (var mod in mods)
                mod.TryResolveDependencies();

            // TODO: Add checking NuGet
            foreach (var mod in mods.Where(mod => !mod.AllDependenciesLoaded))
                Logger.Error(() => $"Couldn't load monkeys for mod [{mod.Title}] because some dependencies weren't present!");

            foreach (var mod in mods.Where(mod => /*mod.AllDependenciesLoaded*/ true))
                mod.LoadEarlyMonkeys();
        }

        /// <summary>
        /// Loads all of the game's assemblies from their potentially modified in-memory versions.
        /// </summary>
        public void LoadGameAssemblies()
        {
            Phase = ExecutionPhase.LoadingGameAssemblies;

            GameAssemblyPool.LoadAll(Locations.PatchedAssemblies);

            Phase = ExecutionPhase.LoadedGameAssemblies;
        }

        /// <summary>
        /// Loads all of the game's assemblies' <see cref="AssemblyDefinition"/>s to potentially modify them in memory.
        /// </summary>
        public void LoadGameAssemblyDefinitions()
        {
            Phase = ExecutionPhase.LoadingGameAssemblyDefinitions;

            foreach (var assemblyFile in Directory.EnumerateFiles(GameAssemblyPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    GameAssemblyPool.LoadDefinition(assemblyFile);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex.LogFormat($"Exception while trying to load assembly {assemblyFile}"));
                }
            }

            var loadedPackages = GameAssemblyPool.GetAllAsLoadedPackages($"{GameName}.").ToArray();
            NuGet.AddAll(loadedPackages);

            foreach (var package in loadedPackages)
                package.TryResolveDependencies();

            //if (!loadedPackages.All(package => package.AllDependenciesLoaded))
            //    throw new InvalidOperationException("Game assemblies contained unresolvable references!");

            Phase = ExecutionPhase.LoadedGameAssemblyDefinitions;
        }

        /// <summary>
        /// Loads every loaded game pack <see cref="RegularMods">mod's</see> pre-patcher assemblies and <see cref="IEarlyMonkey"/>s.
        /// </summary>
        public void LoadGamePackEarlyMonkeys()
        {
            Logger.Info(() => $"Loading every loaded game pack mod's pre-patcher assemblies.");
            LoadEarlyMonkeys(GamePacks);
        }

        /// <summary>
        /// Loads every loaded game pack <see cref="RegularMods">mod's</see> patcher assemblies and <see cref="IMonkey"/>s.
        /// </summary>
        public void LoadGamePackMonkeys()
        {
            Logger.Info(() => $"Loading every loaded game pack mod's patcher assemblies.");
            LoadMonkeys(GamePacks);
        }

        /// <summary>
        /// Loads the mod from the given path, making no checks.
        /// </summary>
        /// <param name="path">The path to the mod file.</param>
        /// <param name="isGamePack">Whether the mod is a game pack.</param>
        /// <returns>The loaded mod.</returns>
        public NuGetPackageMod LoadMod(string path, bool isGamePack = false)
        {
            path = Path.GetFullPath(path);

            Logger.Debug(() => $"Loading {(isGamePack ? "game pack" : "regular")} mod from: {path}");

            var mod = new NuGetPackageMod(this, path, isGamePack);

            AddMod(mod);
            return mod;
        }

        /// <summary>
        /// Loads every loaded regular <see cref="RegularMods">mod's</see> pre-patcher assemblies and <see cref="IEarlyMonkey"/>s.
        /// </summary>
        public void LoadModEarlyMonkeys() => LoadEarlyMonkeys(RegularMods);

        /// <summary>
        /// Loads every loaded regular <see cref="RegularMods">mod's</see> patcher assemblies and <see cref="IMonkey"/>s.
        /// </summary>
        public void LoadModMonkeys() => LoadMonkeys(RegularMods);

        /// <summary>
        /// Loads every given <see cref="Mod"/>'s patcher assemblies and <see cref="IMonkey"/>s.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="IMonkey"/>s to load.</param>
        public void LoadMonkeys(params Mod[] mods) => LoadMonkeys((IEnumerable<Mod>)mods);

        /// <summary>
        /// Loads every given <see cref="Mod"/>'s patcher assemblies and <see cref="IMonkey"/>s.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="IMonkey"/>s to load.</param>
        public void LoadMonkeys(IEnumerable<Mod> mods)
        {
            Logger.Trace(() => "Loading the monkeys of mods in this order:");
            Logger.Trace(mods);

            // TODO: For a FullLoad this shouldn't make a difference since LoadEarlyMonkeys does the same.
            // However users of the library may add more mods inbetween those phases or even later afterwards.
            foreach (var mod in mods)
                mod.TryResolveDependencies();

            // TODO: Add checking NuGet
            foreach (var mod in mods.Where(mod => !mod.AllDependenciesLoaded))
                Logger.Error(() => $"Couldn't load monkeys for mod [{mod.Title}] because some dependencies weren't present!");

            foreach (var mod in mods.Where(mod => /*mod.AllDependenciesLoaded*/ true))
                mod.LoadMonkeys();
        }

        /// <summary>
        /// Loads assembly definitions for runtime assemblies.
        /// </summary>
        public void LoadRuntimeAssemblyDefinitions()
        {
            Phase = ExecutionPhase.LoadingRuntimeAssemblyDefinitions;

            foreach (var assemblyFile in Directory.EnumerateFiles(RuntimeAssemblyPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    RuntimeAssemblyPool.LoadDefinition(assemblyFile);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex.LogFormat($"Exception while trying to load assembly {assemblyFile}"));
                }
            }

            Phase = ExecutionPhase.LoadedRuntimeAssemblyDefinitions;
        }

        /// <summary>
        /// <see cref="Logger.Warn(IEnumerable{object})">Warn</see>-logs all
        /// potentially conflicting <see cref="Harmony">Harmony</see> patches.<br/>
        /// Single source patches are <see cref="Logger.Trace(Func{object})">Trace</see>-logged.
        /// </summary>
        public void LogPotentialConflicts()
        {
            Logger.Info(() => "Checking for potentially conflicting Harmony patches!");
            Logger.Trace(() => "Including all patched methods!");

            foreach (var patchedMethod in Harmony.GetAllPatchedMethods())
            {
                var patches = Harmony.GetPatchInfo(patchedMethod);
                var owners = patches.Owners.ToHashSet();

                string GetLogLine(string harmonyOwner)
                {
                    var name = harmonyOwner;

                    if (this.TryGet<IMonkey>().ByFullId(harmonyOwner, out var monkey))
                        name = monkey.ToString();

                    return $"    [{name}] ({PatchTypesForOwner(patches, harmonyOwner)})";
                }

                // Not sure if this can happen, but just to be sure
                if (owners.Count == 0)
                    continue;

                if (owners.Count == 1)
                {
                    Logger.Trace(() => $"Method \"{patchedMethod.FullDescription()}\" was patched by {GetLogLine(owners.First())}");
                    continue;
                }

                Logger.Warn(() => $"Method \"{patchedMethod.FullDescription()}\" was patched by the following:");

                Logger.Warn(owners.Select(GetLogLine));
            }
        }

        /// <summary>
        /// Runs every given <see cref="Mod"/>'s loaded
        /// <see cref="Mod.EarlyMonkeys">early monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="Mod.EarlyMonkeys">early monkeys</see> should be <see cref="MonkeyBase.Run">run</see>.</param>
        public void RunEarlyMonkeys(params Mod[] mods) => RunEarlyMonkeys((IEnumerable<Mod>)mods);

        /// <summary>
        /// Runs every given <see cref="Mod"/>'s loaded
        /// <see cref="Mod.EarlyMonkeys">early monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="Mod.EarlyMonkeys">early monkeys</see> should be <see cref="MonkeyBase.Run">run</see>.</param>
        public void RunEarlyMonkeys(IEnumerable<Mod> mods)
        {
            // Add check for mod.EarlyMonkeyLoadError

            var earlyMonkeys = mods.GetEarlyMonkeysAscending();

            Logger.Info(() => $"Running {earlyMonkeys.Length} early monkeys!");
            Logger.Trace(() => "Running early monkeys in this order:");
            Logger.Trace(earlyMonkeys);

            var sw = Stopwatch.StartNew();

            foreach (var earlyMonkey in earlyMonkeys)
                earlyMonkey.Run();

            Logger.Info(() => $"Done running early monkeys in {sw.ElapsedMilliseconds}ms!");
        }

        /// <summary>
        /// Runs every loaded <see cref="GamePacks">game pack mod's</see> loaded
        /// <see cref="Mod.EarlyMonkeys">early monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunGamePackEarlyMonkeys()
        {
            Phase = ExecutionPhase.RunningGamePackEarlyMonkeys;

            Logger.Info(() => "Running every loaded game pack mod's loaded early monkeys.");
            RunEarlyMonkeys(GamePacks);

            Phase = ExecutionPhase.RanGamePackEarlyMonkeys;
        }

        /// <summary>
        /// Runs every loaded <see cref="GamePacks">game pack mod's</see> loaded
        /// <see cref="Mod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunGamePackMonkeys()
        {
            Phase = ExecutionPhase.RunningGamePackMonkeys;

            Logger.Info(() => "Running every loaded game pack mod's loaded monkeys.");
            RunMonkeys(GamePacks);

            Phase = ExecutionPhase.RanGamePackMonkeys;
        }

        /// <summary>
        /// <see cref="MonkeyBase.Run">Runs</see> the given <see cref="Mod"/>'s
        /// <see cref="Mod.EarlyMonkeys">early</see> and <see cref="Mod.Monkeys">regular</see> monkeys.
        /// </summary>
        /// <param name="mod">The mod to run.</param>
        /// <exception cref="InvalidOperationException">When the <paramref name="mod"/> mod is invalid.</exception>
        public void RunMod(Mod mod) => RunMods(mod);

        /// <summary>
        /// <see cref="MonkeyBase.Run">Runs</see> the given <see cref="Mod"/>s'
        /// <see cref="Mod.EarlyMonkeys">early</see> and <see cref="Mod.Monkeys">regular</see> monkeys in topological order.
        /// </summary>
        /// <param name="mods">The mods to run.</param>
        /// <exception cref="InvalidOperationException">When <paramref name="mods"/> contains invalid items.</exception>
        public void RunMods(params Mod[] mods)
        {
            var invalidMods = mods.Where(FilterInvalidPresentMod).ToArray();
            if (invalidMods.Length > 0)
                throw new InvalidOperationException($"Attempted to run mod(s) from other loader, that isn't present or was already shut down: {invalidMods.Join()}");

            LoadEarlyMonkeys(mods);
            RunEarlyMonkeys(mods);

            LoadMonkeys(mods);
            RunMonkeys(mods);

            ModsRan?.TryInvokeAll(this, mods);
        }

        /// <summary>
        /// <see cref="MonkeyBase.Run">Runs</see> the given <see cref="Mod"/>s'
        /// <see cref="Mod.EarlyMonkeys">early</see> and <see cref="Mod.Monkeys">regular</see> monkeys in topological order.
        /// </summary>
        /// <param name="mods">The mods to run.</param>
        public void RunMods(IEnumerable<Mod> mods) => RunMods(mods.ToArray());

        /// <summary>
        /// Runs every given <see cref="Mod"/>'s loaded
        /// <see cref="Mod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="Mod.Monkeys">monkeys</see> should be <see cref="MonkeyBase.Run">run</see>.</param>
        public void RunMonkeys(params Mod[] mods) => RunMonkeys((IEnumerable<Mod>)mods);

        /// <summary>
        /// Runs every given <see cref="Mod"/>'s loaded
        /// <see cref="Mod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        /// <param name="mods">The mods who's <see cref="Mod.Monkeys">monkeys</see> should be <see cref="MonkeyBase.Run">run</see>.</param>
        public void RunMonkeys(IEnumerable<Mod> mods)
        {
            // Add check for mod.MonkeyLoadError

            var monkeys = mods.GetMonkeysAscending();

            Logger.Info(() => $"Running {monkeys.Length} monkeys!");
            Logger.Trace(() => "Running monkeys in this order:");
            Logger.Trace(monkeys);

            var sw = Stopwatch.StartNew();

            foreach (var monkey in monkeys)
            {
                if (monkey.Run())
                    continue;

                Logger.Warn(() => "Monkey failed to run, letting it shut down!");

                monkey.Shutdown(false);
            }

            Logger.Info(() => $"Done running monkeys in {sw.ElapsedMilliseconds}ms!");

            LogPotentialConflicts();
        }

        /// <summary>
        /// Runs every loaded <see cref="RegularMods">regular mod's</see> loaded
        /// <see cref="Mod.EarlyMonkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunRegularEarlyMonkeys()
        {
            Phase = ExecutionPhase.RunningRegularEarlyMonkeys;

            Logger.Info(() => "Running every loaded regular mod's loaded early monkeys.");
            RunEarlyMonkeys(RegularMods);

            Phase = ExecutionPhase.RanRegularEarlyMonkeys;
        }

        /// <summary>
        /// Runs every loaded <see cref="RegularMods">regular mod's</see> loaded
        /// <see cref="Mod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunRegularMonkeys()
        {
            Phase = ExecutionPhase.RunningRegularMonkeys;

            Logger.Info(() => "Running every loaded regular mod's loaded monkeys.");
            RunMonkeys(RegularMods);

            Phase = ExecutionPhase.RanRegularMonkeys;
        }

        /// <summary>
        /// Should be called by the game integration or application using this as a library when things are shutting down.<br/>
        /// Saves its config and triggers <see cref="Mod.Shutdown">Shutdown</see>() on all <see cref="RegularMods">Mods</see>.
        /// </summary>
        /// <param name="applicationExiting">Whether the shutdown is caused by the application exiting.</param>
        /// <inheritdoc/>
        public bool Shutdown(bool applicationExiting = true)
        {
            if (ShutdownRan)
            {
                Logger.Warn(() => "This loader's Shutdown() method has already been called!");
                return !ShutdownFailed;
            }

            Logger.Warn(() => $"The loader's shutdown routine was triggered! Triggering shutdown for all {_allMods.Count} mods!");
            Phase = ExecutionPhase.ShuttingDown;
            OnShuttingDown(applicationExiting);

            var sw = Stopwatch.StartNew();

            ShutdownFailed |= !ShutdownMods(_allMods, applicationExiting);

            Logger.Info(() => $"Saving the loader's config!");

            try
            {
                Config.Save();
            }
            catch (Exception ex)
            {
                ShutdownFailed = true;
                Logger.Error(ex.LogFormat("The mod loader's config threw an exception while saving during shutdown!"));
            }

            Logger.Info(() => $"Processed shutdown in {sw.ElapsedMilliseconds}ms!");

            Phase = ExecutionPhase.Shutdown;
            OnShutdownDone(applicationExiting);

            return !ShutdownFailed;
        }

        /// <summary>
        /// Calls the given <see cref="Mod"/>'s <see cref="Mod.Shutdown">Shutdown</see> method
        /// and removes it from this loader's <see cref="Mods">Mods</see>.
        /// </summary>
        /// <param name="mod">The mod to shut down.</param>
        /// <param name="applicationExiting">Whether the shutdown is caused by the application exiting.</param>
        /// <returns><c>true</c> if it the mod belongs to this loader and the shutdown ran successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">When the <paramref name="mod"/> mod is invalid.</exception>
        public bool ShutdownMod(Mod mod, bool applicationExiting = false)
        {
            if (FilterInvalidPresentMod(mod))
                throw new InvalidOperationException($"Attempted to shut down mod from other loader, that isn't present or was already shut down: {mod}");

            ModsShuttingDown?.TryInvokeAll(this, mod.Yield());

            var earlyMonkeys = mod.GetEarlyMonkeysDescending();
            var monkeys = mod.GetMonkeysDescending();

            Logger.Info(() => $"Shutting down {mod} with {earlyMonkeys.Length} early and {monkeys.Length} regular monkeys.");

            var success = true;
            success &= ShutdownMonkeys(earlyMonkeys, monkeys, applicationExiting);
            success &= mod.Shutdown(applicationExiting);

            _allMods.Remove(mod);
            ModsShutdown?.TryInvokeAll(this, mod.Yield());

            return success;
        }

        /// <summary>
        /// Calls the given <see cref="Mod"/>s' <see cref="Mod.Shutdown">Shutdown</see> methods
        /// and removes them from this loader's <see cref="Mods">Mods</see> in reverse topological order.
        /// </summary>
        /// <remarks>
        /// Use the individual <see cref="Mod.ShutdownFailed"/> properties if you want to check <i>which</i> failed.
        /// </remarks>
        /// <param name="applicationExiting">Whether the shutdown is caused by the application exiting.</param>
        /// <param name="mods">The mods to shut down.</param>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">When <paramref name="mods"/> contains invalid items.</exception>
        public bool ShutdownMods(bool applicationExiting = false, params Mod[] mods)
        {
            var invalidMods = mods.Where(FilterInvalidPresentMod).ToArray();
            if (invalidMods.Length > 0)
                throw new InvalidOperationException($"Attempted to shut down mod(s) from other loader, that aren't present or were already shut down: {invalidMods.Join()}");

            var success = true;
            Array.Sort(mods, Mod.DescendingComparer);

            ModsShuttingDown?.TryInvokeAll(this, mods);

            var earlyMonkeys = mods.GetEarlyMonkeysDescending();
            var monkeys = mods.GetMonkeysDescending();

            Logger.Info(() => $"Shutting down {mods.Length} mods with {earlyMonkeys.Length} early and {monkeys.Length} regular monkeys.");

            success &= ShutdownMonkeys(earlyMonkeys, monkeys, applicationExiting);

            Logger.Trace(() => "Shutting down mods in this order:");
            Logger.Trace(mods);

            success &= mods.ShutdownAll(applicationExiting);

            foreach (var mod in mods)
                _allMods.Remove(mod);

            ModsShutdown?.TryInvokeAll(this, mods);

            return success;
        }

        /// <summary>
        /// Calls the given <see cref="Mod"/>s' <see cref="Mod.Shutdown">Shutdown</see> methods
        /// and removes them from this loader's <see cref="Mods">Mods</see> in reverse topological order.
        /// </summary>
        /// <param name="mods">The mods to shut down.</param>
        /// <param name="applicationExiting">Whether the shutdown is caused by the application exiting.</param>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        public bool ShutdownMods(IEnumerable<Mod> mods, bool applicationExiting = false) => ShutdownMods(applicationExiting, mods.ToArray());

        /// <summary>
        /// Searches all of this loader's loaded <see cref="Mods">Mods</see> to find a single one with the given <see cref="Mod.Location">location</see>.
        /// </summary>
        /// <remarks>
        /// If zero or multiple matching mods are found, <paramref name="mod"/> will be <c>null</c>.
        /// </remarks>
        /// <param name="location">The location to find a mod for.</param>
        /// <param name="mod">The mod that was found or <c>null</c>.</param>
        /// <returns><c>true</c> if a mod was found; otherwise, <c>false</c>.</returns>
        public bool TryFindModByLocation(string location, [NotNullWhen(true)] out Mod? mod)
        {
            mod = null;

            if (string.IsNullOrWhiteSpace(location))
            {
                Logger.Warn(() => $"Attempted to get a mod using an invalid location!");
                return false;
            }

            var mods = _allMods.Where(mod => location.Equals(mod.Location, FilesystemComparison)).ToArray();

            if (mods.Length == 0)
                return false;

            if (mods.Length > 1)
            {
                Logger.Error(() => $"Attempted to get multiple mods using path: {location}");
                return false;
            }

            mod = mods[0];
            return true;
        }

        /// <summary>
        /// Tries to get the <see cref="AssemblyDefinition"/> for the given <see cref="AssemblyName"/> from
        /// the <see cref="GameAssemblyPool">GameAssemblyPool</see> or the <see cref="PatcherAssemblyPool">PatcherAssemblyPool</see>.
        /// </summary>
        /// <param name="assemblyName">The assembly to look for.</param>
        /// <param name="assemblyPool">The pool it came from if found, or <c>null</c> otherwise.</param>
        /// <param name="assemblyDefinition">The <see cref="AssemblyDefinition"/> if found, or <c>null</c> otherwise.</param>
        /// <returns>Whether the <see cref="AssemblyDefinition"/> could be returned.</returns>
        public bool TryGetAssemblyDefinition(AssemblyName assemblyName,
            [NotNullWhen(true)] out AssemblyPool? assemblyPool, [NotNullWhen(true)] out AssemblyDefinition? assemblyDefinition)
        {
            lock (this)
            {
                if (GameAssemblyPool.TryWaitForDefinition(assemblyName, out assemblyDefinition))
                {
                    assemblyPool = GameAssemblyPool;
                    return true;
                }

                if (PatcherAssemblyPool.TryWaitForDefinition(assemblyName, out assemblyDefinition))
                {
                    assemblyPool = PatcherAssemblyPool;
                    return true;
                }

                assemblyPool = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to load the given <paramref name="path"/> as a <paramref name="mod"/>
        /// and immediately <see cref="MonkeyBase.Run">runs</see> its
        /// <see cref="Mod.EarlyMonkeys">early</see> and <see cref="Mod.Monkeys">regular</see> monkeys.
        /// </summary>
        /// <param name="path">The path to the file to load as a mod.</param>
        /// <param name="mod">The resulting mod when successful, or null when not.</param>
        /// <param name="isGamePack">Whether the mod is a game pack.</param>
        /// <returns><c>true</c> when the file was successfully loaded; otherwise, <c>false</c>.</returns>
        public bool TryLoadAndRunMod(string path, [NotNullWhen(true)] out NuGetPackageMod? mod, bool isGamePack = false)
        {
            Logger.Debug(() => $"Loading and running {(isGamePack ? "game pack" : "regular")} mod from: {path}");

            if (!TryLoadMod(path, out mod, isGamePack))
                return false;

            RunMod(mod);

            return true;
        }

        /// <summary>
        /// Attempts to load the given <paramref name="path"/> as a <paramref name="mod"/>.
        /// </summary>
        /// <param name="path">The path to the file to load as a mod.</param>
        /// <param name="mod">The resulting mod when successful, or null when not.</param>
        /// <param name="isGamePack">Whether the mod is a game pack.</param>
        /// <returns><c>true</c> when the file was successfully loaded; otherwise, <c>false</c>.</returns>
        public bool TryLoadMod(string path, [NotNullWhen(true)] out NuGetPackageMod? mod, bool isGamePack = false)
        {
            mod = null;

            if (!File.Exists(path))
                return false;

            try
            {
                mod = LoadMod(path, isGamePack);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.LogFormat($"Exception while trying to load mod from {path}:"));
            }

            return false;
        }

        internal void OnAnyConfigChanged(IConfigKeyChangedEventArgs configChangedEvent)
        {
            try
            {
                AnyConfigChanged?.TryInvokeAll(this, configChangedEvent);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex.LogFormat($"Some {nameof(AnyConfigChanged)} event subscriber(s) threw an exception:"));
            }
        }

        private static string GetId(string configPath)
            => Path.GetFileNameWithoutExtension(configPath);

        private static string PatchTypesForOwner(Patches patches, string owner)
        {
            bool OwnerEquals(Patch patch) => Equals(patch.owner, owner);

            var prefixCount = patches.Prefixes.Where(OwnerEquals).Count();
            var postfixCount = patches.Postfixes.Where(OwnerEquals).Count();
            var transpilerCount = patches.Transpilers.Where(OwnerEquals).Count();
            var finalizerCount = patches.Finalizers.Where(OwnerEquals).Count();

            return $"prefix={prefixCount}; postfix={postfixCount}; transpiler={transpilerCount}; finalizer={finalizerCount}";
        }

        private bool FilterInvalidPresentMod(Mod mod)
            => mod.Loader != this || mod.ShutdownRan || !_allMods.Contains(mod);

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

        private bool ShutdownMonkeys(IEarlyMonkey[] earlyMonkeys, IMonkey[] monkeys, bool applicationExiting)
        {
            var success = true;

            Logger.Trace(() => "Shutting down monkeys in this order:");
            Logger.Trace(monkeys);

            success &= monkeys.ShutdownAll(applicationExiting);

            Logger.Trace(() => "Shutting down early monkeys in this order:");
            Logger.Trace(earlyMonkeys);

            success &= earlyMonkeys.ShutdownAll(applicationExiting);

            return success;
        }

        private bool TryLoadGamePack(string path, [NotNullWhen(true)] out NuGetPackageMod? gamePack)
            => TryLoadMod(path, out gamePack, true);

        private bool TryLoadMod(string path, [NotNullWhen(true)] out NuGetPackageMod? mod)
            => TryLoadMod(path, out mod, false);

        /// <summary>
        /// Called when the value of any of this loader's configs changes.<br/>
        /// This gets fired <i>after</i> the source config's <see cref="Config.ItemChanged">ConfigurationChanged</see> event.
        /// </summary>
        public event ConfigKeyChangedEventHandler? AnyConfigChanged;

        /// <summary>
        /// Called when a <see cref="Mod"/> is <see cref="AddMod">added</see> to this loader.
        /// </summary>
        public event ModChangedEventHandler? ModAdded;

        /// <summary>
        /// Called after <see cref="Mod"/>s have been <see cref="RunMods(Mod[])">run</see> by this loader.
        /// </summary>
        public event ModsChangedEventHandler? ModsRan;

        /// <summary>
        /// Called after <see cref="Mod"/>s have been <see cref="Mod.Shutdown">shut down</see> by this loader.
        /// </summary>
        public event ModsChangedEventHandler? ModsShutdown;

        /// <summary>
        /// Called when <see cref="Mod"/>s are about to be <see cref="ShutdownMods(bool, Mod[])">shut down</see> by this loader.
        /// </summary>
        public event ModsChangedEventHandler? ModsShuttingDown;

        /// <inheritdoc/>
        public event ShutdownHandler? ShutdownDone;

        /// <inheritdoc/>
        public event ShutdownHandler? ShuttingDown;

        /// <summary>
        /// Denotes the different stages of the loader's execution.<br/>
        /// Some actions may only work before, in, or after certain phases.
        /// </summary>
        // TODO: Add Phase checks to methods?
        public enum ExecutionPhase
        {
            /// <summary>
            /// Before the constructor has run. Shouldn't be encountered.
            /// </summary>
            Uninitialized,

            /// <summary>
            /// After the constructor has run.
            /// </summary>
            Initialized,

            /// <summary>
            /// While <see cref="LoadRuntimeAssemblyDefinitions"/> is executing.
            /// </summary>
            LoadingRuntimeAssemblyDefinitions,

            /// <summary>
            /// After <see cref="LoadRuntimeAssemblyDefinitions"/> is done.
            /// </summary>
            LoadedRuntimeAssemblyDefinitions,

            /// <summary>
            /// While <see cref="LoadGameAssemblyDefinitions"/> is executing.
            /// </summary>
            LoadingGameAssemblyDefinitions,

            /// <summary>
            /// After <see cref="LoadGameAssemblyDefinitions"/> is done.
            /// </summary>
            LoadedGameAssemblyDefinitions,

            /// <summary>
            /// While <see cref="RunGamePackEarlyMonkeys"/> is executing.
            /// </summary>
            RunningGamePackEarlyMonkeys,

            /// <summary>
            /// After <see cref="RunGamePackEarlyMonkeys"/> is done.
            /// </summary>
            RanGamePackEarlyMonkeys,

            /// <summary>
            /// While <see cref="RunRegularEarlyMonkeys"/> is executing.
            /// </summary>
            RunningRegularEarlyMonkeys,

            /// <summary>
            /// After <see cref="RunRegularEarlyMonkeys"/> is done.
            /// </summary>
            RanRegularEarlyMonkeys,

            /// <summary>
            /// While <see cref="LoadGameAssemblies"/> is executing.
            /// </summary>
            LoadingGameAssemblies,

            /// <summary>
            /// After <see cref="LoadGameAssemblies"/> is done.<br/>
            /// No <see cref="EarlyMonkey{TMonkey}"/>s that target
            /// the now loaded assemblies can work anymore now.
            /// </summary>
            LoadedGameAssemblies,

            /// <summary>
            /// While <see cref="RunGamePackMonkeys"/> is executing.
            /// </summary>
            RunningGamePackMonkeys,

            /// <summary>
            /// After <see cref="RunGamePackMonkeys"/> is done.
            /// </summary>
            RanGamePackMonkeys,

            /// <summary>
            /// While <see cref="RunRegularMonkeys"/> is executing.
            /// </summary>
            RunningRegularMonkeys,

            /// <summary>
            /// After <see cref="RunRegularMonkeys"/> is done.<br/>
            /// This is the active phase until <see cref="MonkeyLoader.Shutdown"/> is triggered.
            /// </summary>
            RanRegularMonkeys,

            /// <summary>
            /// While <see cref="MonkeyLoader.Shutdown"/> is executing.
            /// </summary>
            ShuttingDown,

            /// <summary>
            /// After <see cref="MonkeyLoader.Shutdown"/> is done.<br/>
            /// Nothing should be done with this loader anymore when in this phase.
            /// </summary>
            Shutdown
        }
    }
}