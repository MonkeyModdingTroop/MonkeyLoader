using EnumerableToolkit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Loads or unloads the mod located at the given <paramref name="path"/>.
    /// </summary>
    /// <param name="modLocation">The loading location that triggered the event.</param>
    /// <param name="path">The full name (including path) of the mod file that changed.</param>
    public delegate void HotReloadModEventHandler(ModLoadingLocation modLocation, string path);

    /// <summary>
    /// Specifies where and how to search for mods.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class ModLoadingLocation
    {
        private readonly FileSystemWatcher? _watcher;
        private Regex[] _ignorePatterns;

        /// <summary>
        /// Gets the regex patterns that exclude a mod from being loaded if any match.<br/>
        /// Patterns are matched case-insensitive.
        /// </summary>
        public IEnumerable<Regex> IgnorePatterns
        {
            get => _ignorePatterns.AsSafeEnumerable();

            [MemberNotNull(nameof(_ignorePatterns))]
            set => _ignorePatterns = value.ToArray();
        }

        /// <summary>
        /// Gets the regex patterns that exclude a mod from being loaded if any match as strings.<br/>
        /// Patterns are matched case-insensitive.
        /// </summary>
        [JsonProperty("IgnorePatterns")]
        public IEnumerable<string> IgnorePatternsStrings
        {
            get => _ignorePatterns.Select(regex => regex.ToString());

            [MemberNotNull(nameof(IgnorePatterns), nameof(_ignorePatterns))]
            set => IgnorePatterns = value.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Gets the root folder to search.
        /// </summary>
        [JsonProperty("Path")]
        public string Path { get; }

        /// <summary>
        /// Gets whether nested folders get searched too.
        /// </summary>
        [JsonProperty("Recursive")]
        public bool Recursive { get; }

        /// <summary>
        /// Gets whether a <see cref="FileSystemWatcher"/> gets created to detect changed mods and hot reload them.
        /// </summary>
        [JsonProperty("SupportHotReload")]
        [MemberNotNullWhen(true, nameof(_watcher))]
        public bool SupportHotReload { get; }

        /// <summary>
        /// Creates a new <see cref="ModLoadingLocation"/> with the given specification.
        /// </summary>
        /// <param name="path">The root folder to search.</param>
        /// <param name="recursive">Whether to search nested folders too.</param>
        /// <param name="supportHotReload">Whether a <see cref="FileSystemWatcher"/> gets created to detect changed mods and hot reload them.</param>
        /// <param name="ignorePatterns">Regular expression patterns that exclude a mod from being loaded if any match.<br/>
        /// Patterns are matched case-insensitive.</param>
        [JsonConstructor]
        public ModLoadingLocation(string path, bool recursive, bool supportHotReload, params string[] ignorePatterns)
            : this(path, recursive, supportHotReload, ignorePatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)))
        { }

        /// <summary>
        /// Creates a new <see cref="ModLoadingLocation"/> with the given specification.
        /// </summary>
        /// <param name="path">The root folder to search.</param>
        /// <param name="recursive">Whether to search nested folders too.</param>
        /// <param name="supportHotReload">Whether a <see cref="FileSystemWatcher"/> gets created to detect changed mods and hot reload them.</param>
        /// <param name="ignorePatterns">Regular expression patterns that exclude a mod from being loaded if any match.<br/>
        /// Patterns are matched case-insensitive.</param>
        public ModLoadingLocation(string path, bool recursive, bool supportHotReload, IEnumerable<string> ignorePatterns)
            : this(path, recursive, supportHotReload, ignorePatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)))
        { }

        private ModLoadingLocation(string path, bool recursive, bool supportHotReload, IEnumerable<Regex> ignorePatterns)
        {
            Path = path;
            Recursive = recursive;
            SupportHotReload = supportHotReload;
            IgnorePatterns = ignorePatterns;

            if (!SupportHotReload)
                return;

            _watcher = new FileSystemWatcher(path, NuGetPackageMod.SearchPattern)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = recursive,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            };

            _watcher.Created += OnLoadMod;
            _watcher.Changed += OnReloadMod;
            _watcher.Deleted += OnUnloadMod;
        }

        /// <summary>
        /// Checks that none of the <see cref="IgnorePatterns">IgnorePatterns</see> match the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to check against the <see cref="IgnorePatterns">IgnorePatterns</see>.</param>
        /// <returns><c>true</c> when none match, <c>false</c> otherwise.</returns>
        public bool PassesIgnorePatterns(string path)
            => !_ignorePatterns.Any(pattern => pattern.IsMatch(path));

        /// <summary>
        /// Conducts a search based on the specifications of this loading location.
        /// </summary>
        /// <returns>The full names (including paths) of all files that satisfy the specifications.</returns>
        public IEnumerable<string> Search()
            => Directory.EnumerateFiles(Path, NuGetPackageMod.SearchPattern, Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(PassesIgnorePatterns);

        /// <inheritdoc/>
        public override string ToString()
            => $"[Recursive: {Recursive}, Path: {Path}, Excluding: {{ {string.Join(" ", _ignorePatterns.Select(p => p.ToString()))} }}]";

        private void OnLoadMod(object sender, FileSystemEventArgs e)
        {
            if (PassesIgnorePatterns(e.FullPath))
                LoadMod?.Invoke(this, e.FullPath);
        }

        private void OnReloadMod(object sender, FileSystemEventArgs e)
        {
            OnUnloadMod(sender, e);
            OnLoadMod(sender, e);
        }

        private void OnUnloadMod(object sender, FileSystemEventArgs e)
            => UnloadMod?.Invoke(this, e.FullPath);

        /// <summary>
        /// Called when a mod should be loaded because its got added or changed.
        /// </summary>
        public event HotReloadModEventHandler? LoadMod;

        /// <summary>
        /// Called when a mod should be unloaded because its file got deleted or changed.
        /// </summary>
        public event HotReloadModEventHandler? UnloadMod;
    }
}