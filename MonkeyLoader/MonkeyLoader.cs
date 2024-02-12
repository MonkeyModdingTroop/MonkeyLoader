﻿using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using Mono.Cecil;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// The root of all mod loading.
    /// </summary>
    public sealed class MonkeyLoader : IConfigOwner, IShutdown
    {
        private readonly SortedSet<IModInternal> _allMods = new(Mod.AscendingComparer);
        private LoggingHandler _loggingHandler = MissingLoggingHandler.Instance;

        /// <summary>
        /// Gets the config that this loader uses to load <see cref="ConfigSection"/>s.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Gets the path where the loader's config file should be.
        /// </summary>
        public string ConfigPath { get; }

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of <i>all</i> loaded <see cref="Mods">mods</see>.
        /// </summary>
        public IEnumerable<IEarlyMonkey> EarlyMonkeys => _allMods.SelectMany(mod => mod.EarlyMonkeys);

        /// <summary>
        /// Gets the path pointing of the directory containing the game's assemblies.
        /// </summary>
        public string GameAssemblyPath { get; }

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all loaded <see cref="GamePacks">game pack mods</see>.
        /// </summary>
        public IEnumerable<IEarlyMonkey> GamePackEarlyMonkeys => GamePacks.SelectMany(mod => mod.EarlyMonkeys);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all loaded <see cref="RegularMods">game pack mods</see>.
        /// </summary>
        public IEnumerable<IMonkey> GamePackMonkeys => GamePacks.SelectMany(mod => mod.Monkeys);

        /// <summary>
        /// Gets all loaded game pack <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<IMod> GamePacks => _allMods.Where(mod => mod.IsGamePack);

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
        public LocationConfigSection Locations { get; private set; }

        /// <summary>
        /// Gets the logger that's used by the loader and "inherited" by everything loaded by it.
        /// </summary>
        public MonkeyLogger Logger { get; }

        /// <summary>
        /// Gets or sets the logging handler used by the loader and all <see cref="Mod"/>s loaded by this loader.
        /// </summary>
        public LoggingHandler LoggingHandler
        {
            get => _loggingHandler;
            set
            {
                if (value is null)
                {
                    _loggingHandler = MissingLoggingHandler.Instance;
                    return;
                }

                _loggingHandler = value;

                if (_loggingHandler.Connected)
                    Logger.FlushDeferredMessages();
            }
        }

        /// <summary>
        /// Gets or sets the current <see cref="LoggingLevel"/> used to filter requests on <see cref="MonkeyLogger"/> instances.
        /// </summary>
        public LoggingLevel LoggingLevel { get; set; }

        /// <summary>
        /// Gets <i>all</i> loaded <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<IMod> Mods => _allMods.AsSafeEnumerable();

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of <i>all</i> loaded <see cref="Mods">mods</see>.
        /// </summary>
        public IEnumerable<IMonkey> Monkeys => _allMods.SelectMany(mod => mod.Monkeys);

        /// <summary>
        /// Gets the NuGet manager used by this loader.
        /// </summary>
        public NuGetManager NuGet { get; private set; }

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all loaded <see cref="RegularMods">regular mods</see>.
        /// </summary>
        public IEnumerable<IEarlyMonkey> RegularEarlyMonkeys => RegularMods.SelectMany(mod => mod.EarlyMonkeys);

        /// <summary>
        /// Gets all loaded regular <see cref="Mod"/>s in topological order.
        /// </summary>
        public IEnumerable<IMod> RegularMods => _allMods.Where(mod => !mod.IsGamePack);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all loaded <see cref="RegularMods">regular mods</see>.
        /// </summary>
        public IEnumerable<IMonkey> RegularMonkeys => RegularMods.SelectMany(mod => mod.Monkeys);

        /// <summary>
        /// Gets whether this loaders's <see cref="Shutdown">Shutdown</see>() failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; private set; }

        /// <summary>
        /// Gets whether this loader's <see cref="Shutdown">Shutdown</see>() method has been called.
        /// </summary>
        public bool ShutdownRan { get; private set; }

        internal Queue<MonkeyLogger.DeferredMessage> DeferredMessages { get; } = new();
        internal AssemblyPool GameAssemblyPool { get; }
        internal AssemblyPool PatcherAssemblyPool { get; }

        /// <summary>
        /// Creates a new mod loader with the given configuration file.
        /// </summary>
        /// <param name="configPath">The path to the configuration file to use.</param>
        /// <param name="loggingLevel">The logging level to start with.</param>
        public MonkeyLoader(string configPath = "MonkeyLoader.json", LoggingLevel loggingLevel = LoggingLevel.Info)
        {
            Logger = new(this);
            LoggingLevel = loggingLevel;

            ConfigPath = configPath;
            Id = Path.GetFileNameWithoutExtension(configPath);

            JsonSerializer = new();

            Config = new Config(this);
            Locations = Config.LoadSection<LocationConfigSection>();

            // TODO: also add Zio, Cecil, Harmony, etc.
            NuGet = new NuGetManager(this);
            NuGet.Add(new LoadedNuGetPackage(new PackageIdentity("MonkeyLoader", new NuGetVersion(Assembly.GetExecutingAssembly().GetName().Version)), NuGetHelper.Framework));

            var executablePath = Environment.GetCommandLineArgs()[0];
            GameAssemblyPath = Path.Combine(Path.GetDirectoryName(executablePath), $"{Path.GetFileNameWithoutExtension(executablePath)}_Data", "Managed");

            if (!Directory.Exists(GameAssemblyPath))
                GameAssemblyPath = Path.GetDirectoryName(executablePath);

            GameAssemblyPool = new AssemblyPool(this, "GameAssemblyPool", () => Locations.PatchedAssemblies);
            GameAssemblyPool.AddSearchDirectory(GameAssemblyPath);

            PatcherAssemblyPool = new AssemblyPool(this, "PatcherAssemblyPool", () => Locations.PatchedAssemblies);
            PatcherAssemblyPool.AddFallbackPool(GameAssemblyPool);
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

        public void AddMod(IMod mod)
        {
            Logger.Debug(() => $"Adding {(mod.IsGamePack ? "game pack" : "regular")} mod: {mod.Title}");

            _allMods.Add((IModInternal)mod);
            NuGet.Add(mod);
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
            RunModEarlyMonkeys();

            LoadGameAssemblies();

            LoadGamePackMonkeys();
            RunGamePackMonkeys();

            LoadModMonkeys();
            RunModMonkeys();
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
        /// Loads every given <see cref="IMod"/>'s patcher assemblies and <see cref="IEarlyMonkey"/>s.
        /// </summary>
        public void LoadEarlyMonkeys(IEnumerable<IMod> mods)
        {
            Logger.Trace(() => "Loading early monkeys in this order:");
            Logger.Trace(mods.Cast<IModInternal>().Select(mod => new Func<object>(() => mod.Title)));

            foreach (var mod in mods)
                mod.TryResolveDependencies();

            // TODO: Add checking NuGet
            foreach (var mod in mods.Where(mod => !mod.AllDependenciesLoaded))
                Logger.Error(() => $"Couldn't load monkeys for mod [{mod.Title}] because some dependencies weren't present!");

            foreach (var mod in mods.Where(mod => mod.AllDependenciesLoaded).Cast<IModInternal>())
                mod.LoadEarlyMonkeys();
        }

        /// <summary>
        /// Loads all of the game's assemblies from their potentially modified in-memory versions.
        /// </summary>
        public void LoadGameAssemblies()
        {
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
        }

        public void LoadGameAssemblyDefinitions()
        {
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

            var loadedPackages = GameAssemblyPool.GetAllAsLoadedPackages().ToArray();
            NuGet.AddAll(loadedPackages);

            foreach (var package in loadedPackages)
                package.TryResolveDependencies();

            //if (!loadedPackages.All(package => package.AllDependenciesLoaded))
            //    throw new InvalidOperationException("Game assemblies contained unresolvable references!");
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
        /// Loads every given <see cref="IMod"/>'s patcher assemblies and <see cref="IMonkey"/>s.
        /// </summary>
        public void LoadMonkeys(IEnumerable<IMod> mods)
        {
            Logger.Trace(() => "Loading monkeys in this order:");
            Logger.Trace(mods.Cast<IModInternal>().Select(mod => new Func<object>(() => mod.Title)));

            // TODO: For a FullLoad this shouldn't make a difference since LoadEarlyMonkeys does the same.
            // However users of the library may add more mods inbetween those phases or even later afterwards.
            foreach (var mod in mods)
                mod.TryResolveDependencies();

            // TODO: Add checking NuGet
            foreach (var mod in mods.Where(mod => !mod.AllDependenciesLoaded))
                Logger.Error(() => $"Couldn't load monkeys for mod [{mod.Title}] because some dependencies weren't present!");

            foreach (var mod in mods.Where(mod => mod.AllDependenciesLoaded).Cast<IModInternal>())
                mod.LoadMonkeys();
        }

        /// <summary>
        /// Runs every given <see cref="RegularMods">mod's</see> loaded
        /// <see cref="IEarlyMonkey"/>s <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunEarlyMonkeys(IEnumerable<IMod> mods)
        {
            // Add check for mod.EarlyMonkeyLoadError

            var earlyMonkeys = mods.SelectMany(mod => mod.EarlyMonkeys).ToArray();
            Array.Sort(earlyMonkeys, Monkey.AscendingComparer);

            Logger.Info(() => $"Running {earlyMonkeys.Length} early monkeys!");
            Logger.Trace(() => "Running early monkeys in this order:");
            Logger.Trace(earlyMonkeys.Select(eM => new Func<object>(() => $"{eM.Mod.Title}/{eM.Name}")));

            var sw = Stopwatch.StartNew();

            foreach (var earlyMonkey in earlyMonkeys)
                earlyMonkey.Run();

            Logger.Info(() => $"Done running early monkeys in {sw.ElapsedMilliseconds}ms!");
        }

        /// <summary>
        /// Runs every loaded game pack <see cref="RegularMods">mod's</see> loaded <see cref="IEarlyMonkey"/>s.
        /// </summary>
        public void RunGamePackEarlyMonkeys()
        {
            Logger.Info(() => "Running every loaded game pack mod's loaded early monkeys.");
            RunEarlyMonkeys(GamePacks);
        }

        /// <summary>
        /// Runs every loaded game pack <see cref="RegularMods">mod's</see> loaded
        /// <see cref="IMod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunGamePackMonkeys()
        {
            Logger.Info(() => "Running every loaded game pack mod's loaded monkeys.");
            RunMonkeys(GamePacks);
        }

        /// <summary>
        /// Runs every loaded regular <see cref="RegularMods">mod's</see> loaded <see cref="IEarlyMonkey"/>s.
        /// </summary>
        public void RunModEarlyMonkeys()
        {
            Logger.Info(() => "Running every loaded regular mod's loaded early monkeys.");
            RunEarlyMonkeys(RegularMods);
        }

        /// <summary>
        /// Runs every loaded regular <see cref="RegularMods">mod's</see> loaded
        /// <see cref="IMod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunModMonkeys()
        {
            Logger.Info(() => "Running every loaded regular mod's loaded monkeys.");
            RunMonkeys(RegularMods);
        }

        /// <summary>
        /// Runs every given <see cref="IMod"/>'s loaded
        /// <see cref="IMod.Monkeys">monkeys'</see> <see cref="MonkeyBase.Run">Run</see>() method.
        /// </summary>
        public void RunMonkeys(IEnumerable<IMod> mods)
        {
            // Add check for mod.MonkeyLoadError

            var monkeys = mods.SelectMany(mod => mod.Monkeys).ToArray();
            Array.Sort(monkeys, Monkey.AscendingComparer);

            Logger.Info(() => $"Running {monkeys.Length} early monkeys!");
            Logger.Trace(() => "Running monkeys in this order:");
            Logger.Trace(monkeys.Select(m => new Func<object>(() => $"{m.Mod.Title}/{m.Name}")));

            var sw = Stopwatch.StartNew();

            foreach (var monkey in monkeys)
                monkey.Run();

            Logger.Info(() => $"Done running monkeys in {sw.ElapsedMilliseconds}ms!");
        }

        /// <summary>
        /// Should be called by the game integration or application using this as a library when things are shutting down.<br/>
        /// Saves its config and triggers <see cref="Mod.Shutdown">Shutdown</see>() on all <see cref="RegularMods">Mods</see>.
        /// </summary>
        /// <inheritdoc/>
        public bool Shutdown()
        {
            if (ShutdownRan)
                Logger.Warn(() => "This loader's Shutdown() method has already been called!");
            //throw new InvalidOperationException("A loader's Shutdown() method must only be called once!");

            ShutdownRan = true;

            var sw = Stopwatch.StartNew();
            Logger.Info(() => $"Triggering shutdown routine! Saving the loader's config.");

            try
            {
                Logger.Debug(() => $"Triggering save for the mod loader's config to shut down!");
                Config.Save();
            }
            catch (Exception ex)
            {
                ShutdownFailed = true;
                Logger.Error(() => ex.Format("The mod loader's config threw an exception while saving during shutdown!"));
            }

            Logger.Info(() => $"Triggering shutdown for all {_allMods.Count} mods!");

            foreach (var mod in _allMods)
                ShutdownFailed |= !mod.Shutdown();

            Logger.Info(() => $"Processed shutdown in {sw.ElapsedMilliseconds}ms!");

            return !ShutdownFailed;
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
        /// Attempts to load the given <paramref name="path"/> as a <paramref name="mod"/>.
        /// </summary>
        /// <param name="path">The path to the file to load as a mod.</param>
        /// <param name="mod">The resulting mod when successful, or null when not.</param>
        /// <param name="isGamePack">Whether the mod is a game pack.</param>
        /// <returns><c>true</c> when the file was successfully loaded.</returns>
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

        private bool TryLoadGamePack(string path, [NotNullWhen(true)] out NuGetPackageMod? gamePack)
            => TryLoadMod(path, out gamePack, true);

        private bool TryLoadMod(string path, [NotNullWhen(true)] out NuGetPackageMod? mod)
            => TryLoadMod(path, out mod, false);

        /// <summary>
        /// Called when the value of any of this loader's configs changes.<br/>
        /// This gets fired <i>after</i> the source config's <see cref="Config.ItemChanged">ConfigurationChanged</see> event.
        /// </summary>
        public event ConfigKeyChangedEventHandler? AnyConfigChanged;
    }
}