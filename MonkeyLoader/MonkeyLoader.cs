using MonkeyLoader.Configuration;
using MonkeyLoader.Events;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using Mono.Cecil;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
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
    public sealed class MonkeyLoader : IConfigOwner, IShutdown
    {
        /// <summary>
        /// All the currently loaded and still active mods of this loader, kept in topological order.
        /// </summary>
        private readonly SortedSet<Mod> _allMods = new(Mod.AscendingComparer);

        private ExecutionPhase _phase;

        /// <summary>
        /// Gets the config that this loader uses to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets the path where the loader's config file should be.
        /// </summary>
        public string ConfigPath { get; }

        /// <summary>
        /// Gets the path pointing of the directory containing the game's assemblies.
        /// </summary>
        public string GameAssemblyPath { get; }

        /// <summary>
        /// Gets the name of the game (its executable).
        /// </summary>
        public string GameName { get; }

        /// <summary>
        /// Gets all loaded game pack <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<Mod> GamePacks => _allMods.Where(mod => mod.IsGamePack);

        /// <summary>
        /// Gets this loader's id.
        /// </summary>
        public string Id { get; }

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
        /// Gets the <see cref="LoggingController"/> used by this loader and everything loaded by it.
        /// </summary>
        public LoggingController LoggingController { get; }

        /// <summary>
        /// Gets <i>all</i> loaded <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<Mod> Mods => _allMods.AsSafeEnumerable();

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

        /// <summary>
        /// Creates a new mod loader with the given configuration file.
        /// </summary>
        /// <param name="configPath">The path to the configuration file to use.</param>
        /// <param name="loggingController">The logging controller that this loader should use or a default one when <c>null</c>.</param>
        public MonkeyLoader(string configPath = "MonkeyLoader/MonkeyLoader.json", LoggingController? loggingController = null)
        {
            ConfigPath = configPath;
            Id = Path.GetFileNameWithoutExtension(configPath);

            LoggingController = loggingController ?? new LoggingController(Id);
            Logger = new(LoggingController);

            JsonSerializer = new();

            Config = new Config(this);
            Locations = Config.LoadSection<LocationConfigSection>();

            foreach (var modLocation in Locations.Mods)
            {
                modLocation.LoadMod += (mL, path) => TryLoadAndRunMod(path, out _);
                modLocation.UnloadMod += (mL, path) =>
                {
                    if (TryFindModByLocation(path, out var mod))
                        ShutdownMod(mod);
                };
            }

            // TODO: do this properly - scan all loaded assemblies?
            NuGet = new NuGetManager(this);
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("MonkeyLoader", new NuGetVersion(Assembly.GetExecutingAssembly().GetName().Version)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Newtonsoft.Json", new NuGetVersion(13, 0, 3)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("NuGet.Packaging", new NuGetVersion(6, 9, 1)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("NuGet.Protocol", new NuGetVersion(6, 9, 1)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Mono.Cecil", new NuGetVersion(0, 11, 5)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Harmony", new NuGetVersion(2, 3, 0)), NuGetHelper.Framework));
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("Zio", new NuGetVersion(0, 17, 0)), NuGetHelper.Framework));

            var executablePath = Environment.GetCommandLineArgs()[0];
            GameName = Path.GetFileNameWithoutExtension(executablePath);
            GameAssemblyPath = Path.Combine(Path.GetDirectoryName(executablePath), $"{GameName}_Data", "Managed");

            if (!Directory.Exists(GameAssemblyPath))
                GameAssemblyPath = Path.GetDirectoryName(executablePath);

            GameAssemblyPool = new AssemblyPool(this, "GameAssemblyPool", () => Locations.PatchedAssemblies);
            GameAssemblyPool.AddSearchDirectory(GameAssemblyPath);

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
            foreach (var type in assembly.GetTypes().Instantiable<JsonConverter>())
            {
                if (type.GetCustomAttribute<IgnoreJsonConverterAttribute>() is null)
                    AddJsonConverter(type);
            }
        }

        /// <summary>
        /// Adds a mod to be managed by this loader.
        /// </summary>
        /// <param name="mod">The mod to add.</param>
        public void AddMod(Mod mod)
        {
            Logger.Debug(() => $"Adding {(mod.IsGamePack ? "game pack" : "regular")} mod: {mod.Title}");

            _allMods.Add(mod);
            NuGet.Add(mod);

            ModAdded?.TryInvokeAll(this, mod);
        }

        /// <summary>
        /// Tries to create all <see cref="Locations">Locations</see> used by this loader.
        /// </summary>
        public void EnsureAllLocationsExist()
        {
            IEnumerable<string> locations = new[] { Locations.Configs, Locations.GamePacks, Locations.Libs, Locations.PatchedAssemblies };
            var modLocations = Locations.Mods.Select(modLocation => modLocation.Path).ToArray();

            Logger.Info(() => $"Ensuring that all configured locations exist as directories:{Environment.NewLine}" +
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
                    Logger.Error(() => ex.Format($"Exception while trying to create directory: {location}"));
                }
            }
        }

        /// <summary>
        /// Performs the full loading routine without customizations or interventions.
        /// </summary>
        public void FullLoad()
        {
            EnsureAllLocationsExist();
            LoadGameAssemblyDefinitions();

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
                Logger.Error(() => ex.Format($"Exception while searching files at location {Locations.GamePacks}:"));
                return Enumerable.Empty<NuGetPackageMod>();
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
                    Logger.Error(() => ex.Format($"Exception while searching files at location {location}:"));
                }

                return Enumerable.Empty<string>();
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

            foreach (var mod in mods.Where(mod => mod.AllDependenciesLoaded))
                mod.LoadEarlyMonkeys();
        }

        /// <summary>
        /// Loads all of the game's assemblies from their potentially modified in-memory versions.
        /// </summary>
        public void LoadGameAssemblies()
        {
            Phase = ExecutionPhase.LoadingGameAssemblies;

            GameAssemblyPool.LoadAll(Locations.PatchedAssemblies);

            // Load all unmodified assemblies that weren't loaded already
            foreach (var assemblyFile in Directory.EnumerateFiles(GameAssemblyPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    Assembly.LoadFile(assemblyFile);
                }
                catch (Exception ex)
                {
                    Logger.Debug(() => ex.Format($"Exception while trying to load assembly {assemblyFile}"));
                }
            }

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
                    Logger.Debug(() => ex.Format($"Exception while trying to load assembly {assemblyFile}"));
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

            foreach (var mod in mods.Where(mod => mod.AllDependenciesLoaded))
                mod.LoadMonkeys();
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
        public void RunMod(Mod mod) => RunMods(mod);

        /// <summary>
        /// <see cref="MonkeyBase.Run">Runs</see> the given <see cref="Mod"/>s'
        /// <see cref="Mod.EarlyMonkeys">early</see> and <see cref="Mod.Monkeys">regular</see> monkeys in topological order.
        /// </summary>
        /// <param name="mods">The mods to run.</param>
        public void RunMods(params Mod[] mods)
        {
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
                monkey.Run();

            Logger.Info(() => $"Done running monkeys in {sw.ElapsedMilliseconds}ms!");
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
                Logger.Error(() => ex.Format("The mod loader's config threw an exception while saving during shutdown!"));
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
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        public bool ShutdownMod(Mod mod, bool applicationExiting = false)
        {
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
        /// <param name="location"></param>
        /// <param name="mod">The mod that was found.</param>
        /// <returns>Whether a single matching mod was found.</returns>
        public bool TryFindModByLocation(string location, [NotNullWhen(true)] out Mod? mod)
        {
            mod = null;

            if (string.IsNullOrWhiteSpace(location))
            {
                Logger.Warn(() => $"Attempted to get a mod using an invalid location!");
                return false;
            }

            var mods = _allMods.Where(mod => location.Equals(mod.Location, StringComparison.Ordinal)).ToArray();

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
                Logger.Error(() => ex.Format($"Exception while trying to load mod from {path}:"));
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
                Logger.Error(() => ex.Format($"Some {nameof(AnyConfigChanged)} event subscriber(s) threw an exception:"));
            }
        }

        private void OnShutdownDone(bool applicationExiting)
        {
            try
            {
                ShutdownDone?.TryInvokeAll(this, applicationExiting);
            }
            catch (AggregateException ex)
            {
                Logger.Error(() => ex.Format($"Some {nameof(ShutdownDone)} event subscriber(s) threw an exception:"));
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
                Logger.Error(() => ex.Format($"Some {nameof(ShuttingDown)} event subscriber(s) threw an exception:"));
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