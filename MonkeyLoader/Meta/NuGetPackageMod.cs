using EnumerableToolkit;
using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using Mono.Cecil;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zio;
using Zio.FileSystems;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Contains all the metadata and references to loaded patchers from a .nupkg mod file.
    /// </summary>
    public sealed class NuGetPackageMod : Mod
    {
        /// <summary>
        /// The search pattern for mod files supported by this mod-type.
        /// </summary>
        public const string SearchPattern = "*.nupkg";

        internal readonly HashSet<Assembly> PatcherAssemblies = new();
        internal readonly HashSet<Assembly> PrePatcherAssemblies = new();

        private Dictionary<string, Assembly> assemblyCache = new();
        private const char AuthorsSeparator = ',';
        private const string PrePatchersFolderName = "pre-patchers";
        private const char TagsSeparator = ' ';

        private readonly string? _title;

        /// <inheritdoc/>
        public override string Description { get; }

        /// <inheritdoc/>
        public override IFileSystem FileSystem { get; }

        /// <inheritdoc/>
        public override UPath? IconPath { get; }

        /// <inheritdoc/>
        public override Uri? IconUrl { get; }

        /// <inheritdoc/>
        public override PackageIdentity Identity { get; }

        /// <summary>
        /// Gets the paths inside this mod's <see cref="FileSystem">FileSystem</see> that point to patcher assemblies that should be loaded.
        /// </summary>
        public IEnumerable<UPath> PatcherAssemblyPaths => assemblyPaths.Where(path => !path.FullName.Contains(PrePatchersFolderName));

        /// <summary>
        /// Gets the paths inside this mod's <see cref="FileSystem">FileSystem</see> that point to pre-patcher assemblies that should be loaded.
        /// </summary>
        public IEnumerable<UPath> PrePatcherAssemblyPaths => assemblyPaths.Where(path => path.FullName.Contains(PrePatchersFolderName));

        /// <inheritdoc/>
        public override Uri? ProjectUrl { get; }

        /// <inheritdoc/>
        public override string? ReleaseNotes { get; }

        /// <inheritdoc/>
        public override bool SupportsHotReload => true;

        /// <inheritdoc/>
        public override NuGetFramework TargetFramework { get; }

        /// <inheritdoc/>
        public override string Title => _title ?? base.Title;

        /// <summary>
        /// Creates a new <see cref="NuGetPackageMod"/> instance for the given <paramref name="loader"/>,
        /// loading a .nupkg into memory from the given <paramref name="location"/>.<br/>
        /// The metadata gets loaded from a <c>.nuspec</c> file, which must be at the root of the file system.
        /// </summary>
        /// <param name="loader">The loader instance that loaded this mod.</param>
        /// <param name="location">The absolute file path to the mod's file.</param>
        /// <param name="isGamePack">Whether this mod is a game pack.</param>
        public NuGetPackageMod(MonkeyLoader loader, string location, bool isGamePack) : base(loader, location, isGamePack)
        {
            using var fileStream = File.OpenRead(location);
            var memoryStream = new MemoryStream((int)fileStream.Length);
            fileStream.CopyTo(memoryStream);

            var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read, true);
            var packageReader = new PackageArchiveReader(zipArchive, NuGetHelper.NameProvider, NuGetHelper.CompatibilityProvider);
            FileSystem = new ZipArchiveFileSystem(zipArchive);

            var nuspecReader = packageReader.NuspecReader;

            var title = nuspecReader.GetTitle();
            _title = string.IsNullOrWhiteSpace(title) ? null : title;

            Identity = nuspecReader.GetIdentity();

            Description = nuspecReader.GetDescription();
            ReleaseNotes = nuspecReader.GetReleaseNotes();

            tags.AddRange(nuspecReader.GetTags().Split(new[] { TagsSeparator }, StringSplitOptions.RemoveEmptyEntries));
            authors.AddRange(nuspecReader.GetAuthors().Split(new[] { AuthorsSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim()));

            if (!string.IsNullOrWhiteSpace(nuspecReader.GetIcon()))
            {
                var iconPath = new UPath(nuspecReader.GetIcon()).ToAbsolute();

                if (FileSystem.FileExists(iconPath))
                    IconPath = iconPath;
                else
                    Logger.Warn(() => $"Icon Path [{iconPath}] is set but the file doesn't exist for mod: {location}");
            }

            var iconUrl = nuspecReader.GetIconUrl();
            if (Uri.TryCreate(iconUrl, UriKind.Absolute, out var iconUri))
                IconUrl = iconUri;
            else if (!string.IsNullOrWhiteSpace(iconUrl))
                Logger.Warn(() => $"Icon Url [{iconUrl}] is set but is invalid for mod: {location}");

            var projectUrl = nuspecReader.GetProjectUrl();
            if (Uri.TryCreate(projectUrl, UriKind.Absolute, out var projectUri))
                ProjectUrl = projectUri;
            else if (!string.IsNullOrWhiteSpace(projectUrl))
                Logger.Warn(() => $"Project Url [{projectUrl}] is set but is invalid for mod: {location}");

            var contentItemGroups = packageReader.GetContentItems().ToArray();
            var nearestContentItems = contentItemGroups.GetNearestCompatible();
            var anyContentItems = contentItemGroups.GetNearest(NuGetFramework.AnyFramework);

            if (nearestContentItems is not null)
                contentPaths.AddRange(nearestContentItems.Items.Select(path => new UPath(path).ToAbsolute()));

            if (anyContentItems is not null && anyContentItems != nearestContentItems)
                contentPaths.AddRange(anyContentItems.Items.Select(path => new UPath(path).ToAbsolute()));

            var nearestLib = packageReader.GetLibItems().GetNearestCompatible();
            if (nearestLib is null)
            {
                TargetFramework = NuGetFramework.AnyFramework;
                Logger.Warn(() => $"No compatible lib entry found!");

                return;
            }

            TargetFramework = nearestLib.TargetFramework;
            Logger.Debug(() => $"Nearest compatible lib entry: {nearestLib.TargetFramework}");

            assemblyPaths.AddRange(nearestLib.Items
                .Where(path => AssemblyExtension.Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase))
                .Select(path => new UPath(path).ToAbsolute()));

            if (assemblyPaths.Any())
                Logger.Trace(() => $"Found the following assemblies:{Environment.NewLine}    - {string.Join($"{Environment.NewLine}    - ", assemblyPaths)}");
            else
                Logger.Warn(() => "Found no assemblies!");

            var deps = packageReader.GetPackageDependencies()
                .SingleOrDefault(group => TargetFramework.Equals(group.TargetFramework))
                ?? packageReader.GetPackageDependencies().SingleOrDefault(group => NuGetFramework.AnyFramework.Equals(group.TargetFramework));

            if (deps is null)
                return;

            foreach (var package in deps.Packages)
                dependencies.Add(package.Id, new DependencyReference(loader.NuGet, package));

            if (dependencies.Any())
                Logger.Debug(() => $"Found the following dependencies:{Environment.NewLine}    - {string.Join($"{Environment.NewLine}    - ", dependencies.Keys)}");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        /// <inheritdoc/>
        protected override bool OnLoadEarlyMonkeys()
        {
            var error = false;

            foreach (var prepatcherPath in PrePatcherAssemblyPaths)
            {
                try
                {
                    Logger.Debug(() => $"Loading pre-patcher assembly from: {prepatcherPath}");

                    var assembly = LoadAssembly(FileSystem, prepatcherPath);
                    Loader.AddJsonConverters(assembly);
                    PrePatcherAssemblies.Add(assembly);

                    var instantiableTypes = assembly.GetTypes().ParameterlessInstantiable<IEarlyMonkey>().ToArray();
                    Logger.Trace(() => $"Found the following instantiable EarlyMonkey Types:{Environment.NewLine}    - {string.Join($"{Environment.NewLine}    - ", instantiableTypes.Select(t => t.FullName))}");

                    foreach (var type in instantiableTypes)
                    {
                        Logger.Debug(() => $"Instantiating EarlyMonkey Type: {type.FullName}");
                        earlyMonkeys.Add(MonkeyBase.GetInstance<IEarlyMonkey>(type, this));
                    }

                    Logger.Info(() => $"Found {earlyMonkeys.Count} Early Monkeys!");
                }
                catch (Exception ex)
                {
                    error = true;
                    Logger.Error(ex.LogFormat($"Error while loading Early Monkeys from assembly: {prepatcherPath}!"));
                }
            }

            return !error;
        }

        /// <inheritdoc/>
        protected override bool OnLoadMonkeys()
        {
            // assemblies should be Mono.Cecil loaded before the Early ones, to allow pre-patchers access

            var error = false;

            foreach (var patcherPath in PatcherAssemblyPaths)
            {
                try
                {
                    Logger.Debug(() => $"Loading patcher assembly from: {patcherPath}");

                    var assembly = LoadAssembly(FileSystem, patcherPath);
                    Loader.AddJsonConverters(assembly);
                    PatcherAssemblies.Add(assembly);

                    Logger.Info(() => $"Loaded patcher assembly: {assembly.FullName}");

                    var instantiableTypes = assembly.GetTypes().ParameterlessInstantiable<MonkeyBase>();
                    Logger.Trace(() => $"Found the following instantiable Monkey Types:{Environment.NewLine}    - {string.Join($"{Environment.NewLine}    - ", instantiableTypes.Select(t => t.FullName))}");

                    foreach (var type in instantiableTypes)
                    {
                        Logger.Debug(() => $"Instantiating Monkey Type: {type.FullName}");
                        monkeys.Add(MonkeyBase.GetInstance<IMonkey>(type, this));
                    }

                    Logger.Info(() => $"Found {monkeys.Count} Monkeys!");
                }
                catch (Exception ex)
                {
                    error = true;
                    Logger.Error(ex.LogFormat($"Error while loading Monkeys from assembly: {patcherPath}!"));
                }
            }

            return !error;
        }
        
        private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
        {
            var strippedName = args.Name[..args.Name.IndexOf(',')];
            if (assemblyCache.TryGetValue(strippedName, out var cachedAssembly))
            {
                Logger.Debug(() => $"Resolving assembly: {args.Name} from cache");
                return cachedAssembly;
            }
                
            var assemblyPath = assemblyPaths.FirstOrDefault(path => path.GetNameWithoutExtension() == strippedName);

            if (assemblyPath == default)
                return null;

            Logger.Debug(() => $"Resolving assembly: {args.Name} from path: {assemblyPath}");
            return LoadAssembly(FileSystem, assemblyPath);
        }

        private Assembly LoadAssembly(IFileSystem fileSystem, UPath assemblyPath)
        {
            var filename = assemblyPath.GetNameWithoutExtension()!;
            
            if (!fileSystem.FileExists(assemblyPath))
            {
                return null!;
            }

            using var assemblyFile = fileSystem.OpenFile(assemblyPath, FileMode.Open, FileAccess.Read);
            using var assemblyStream = new MemoryStream();
            assemblyFile.CopyTo(assemblyStream);

            var mdbPath = assemblyPath + ".mdb";
            var pdbPath = assemblyPath.GetDirectory() / $"{filename}.pdb";
            using var symbolStream = new MemoryStream();

            if (fileSystem.FileExists(mdbPath))
            {
                using var mdbFile = fileSystem.OpenFile(mdbPath, FileMode.Open, FileAccess.Read);
                mdbFile.CopyTo(symbolStream);
            }
            else if (fileSystem.FileExists(pdbPath))
            {
                using var pdbFile = fileSystem.OpenFile(pdbPath, FileMode.Open, FileAccess.Read);
                pdbFile.CopyTo(symbolStream);
            }

            // TODO: Only when hot reloadable
            assemblyStream.Position = 0;
            //var assemblyDefinition = Loader.PatcherAssemblyPool.LoadDefinition(assemblyStream);
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyStream);
            var newAssemblyStream = new MemoryStream();

            var loadedModules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetLoadedModules()).Select(module => module.ModuleVersionId).ToHashSet();

            var anyConflicting = false;
            foreach (var conflictingModule in assemblyDefinition.Modules.Where(moduleDef => loadedModules.Contains(moduleDef.Mvid)))
            {
                anyConflicting = true;
                conflictingModule.Mvid = Guid.NewGuid();
            }

            if (anyConflicting || AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyDefinition.Name.Name))
            {
                assemblyDefinition.Name.Name += $"-{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
                assemblyDefinition.Write(newAssemblyStream);
            }

            var assembly = Assembly.Load((newAssemblyStream.Length == 0 ? assemblyStream : newAssemblyStream).ToArray(), symbolStream.ToArray());
            assemblyCache[assembly.GetName().Name] = assembly;
            return assembly;
        }
    }
}