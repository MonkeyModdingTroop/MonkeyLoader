using MonkeyLoader.NuGet;
using MonkeyLoader.Patching;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using Zio;
using Zio.FileSystems;

namespace MonkeyLoader.Meta
{
    /// <summary>
    /// Contains all the metadata and references to patchers, which can be constructed dynamically at runtime using <see cref="Builder"/>.
    /// </summary>
    public class DynamicMod : Mod
    {
        private readonly Type[] _earlyMonkeyTypes;
        private readonly Type[] _monkeyTypes;
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

        /// <inheritdoc/>
        public override Uri? ProjectUrl { get; }

        /// <inheritdoc/>
        public override string? ReleaseNotes { get; }

        /// <inheritdoc/>
        public override NuGetFramework TargetFramework => NuGetHelper.Framework;

        /// <inheritdoc/>
        public override string Title => _title ?? base.Title;

        private DynamicMod(MonkeyLoader loader, Builder builder)
            : base(loader, builder.Location, builder.IsGamePack)
        {
            authors.AddRange(builder.Authors);
            Description = builder.Description;
            FileSystem = builder.FileSystem;
            IconPath = builder.IconPath;
            IconUrl = builder.IconUrl;
            Identity = new PackageIdentity(builder.Id, new NuGetVersion(builder.Version));
            ProjectUrl = builder.ProjectUrl;
            ReleaseNotes = builder.ReleaseNotes;
            tags.AddRange(builder.Tags);
            _title = builder.Title;

            _monkeyTypes = builder.Monkeys.ToArray();
            _earlyMonkeyTypes = builder.EarlyMonkeys.ToArray();
        }

        /// <inheritdoc/>
        protected override bool OnLoadEarlyMonkeys()
        {
            foreach (var earlyMonkeyType in _earlyMonkeyTypes)
            {
                Logger.Debug(() => $"Instantiating Monkey Type: {earlyMonkeyType.FullName}");
                monkeys.Add(MonkeyBase.GetInstance<IEarlyMonkey>(earlyMonkeyType, this));
            }

            return true;
        }

        /// <inheritdoc/>
        protected override bool OnLoadMonkeys()
        {
            foreach (var monkeyType in _monkeyTypes)
            {
                Logger.Debug(() => $"Instantiating Monkey Type: {monkeyType.FullName}");
                monkeys.Add(MonkeyBase.GetInstance<IMonkey>(monkeyType, this));
            }

            return true;
        }

        /// <summary>
        /// Use this to construct a <see cref="DynamicMod"/>.
        /// </summary>
        public sealed class Builder
        {
            private readonly List<Type> _earlyMonkeyTypes = new();
            private readonly List<Type> _monkeyTypes = new();
            private bool _created = false;

            /// <summary>
            /// Gets or sets the names of the authors of this mod.
            /// </summary>
            public IEnumerable<string> Authors { get; set; } = Array.Empty<string>();

            /// <summary>
            /// Gets or sets the description of this mod.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Gets or sets the types of the <see cref="IEarlyMonkey"/>s that should be part of this mod.
            /// </summary>
            public IEnumerable<Type> EarlyMonkeys => _earlyMonkeyTypes.AsSafeEnumerable();

            /// <summary>
            /// Gets the file system for the mod.
            /// </summary>
            public IFileSystem FileSystem { get; }

            /// <summary>
            /// Gets or sets the path to the mod's icon inside the mod's <see cref="FileSystem">FileSystem</see>.<br/>
            /// <c>null</c> if it wasn't given or doesn't exist.
            /// </summary>
            public UPath? IconPath { get; set; }

            /// <summary>
            /// Gets or sets the Url to the mod's icon on the web.<br/>
            /// <c>null</c> if it wasn't given or was invalid.
            /// </summary>
            public Uri? IconUrl { get; set; }

            /// <summary>
            /// Gets the unique identifier of this mod.
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// Gets or sets whether this mod is a game pack.
            /// </summary>
            /// <remarks>
            /// <i>Default:</i> <c>false</c>
            /// </remarks>
            public bool IsGamePack { get; set; } = false;

            /// <summary>
            /// Gets or sets the absolute path to this mod's file. May be <c>null</c> if the mod only exists in memory.
            /// </summary>
            public string? Location { get; set; }

            /// <summary>
            /// Gets or sets the types of the <see cref="IMonkey"/>s that should be part of this mod.
            /// </summary>
            public IEnumerable<Type> Monkeys => _monkeyTypes.AsSafeEnumerable();

            /// <summary>
            /// Gets or sets the Url to this mod's project website.<br/>
            /// <c>null</c> if it wasn't given or was invalid.
            /// </summary>
            public Uri? ProjectUrl { get; set; }

            /// <summary>
            /// Gets or sets the release notes for this mod's version.
            /// </summary>
            public string? ReleaseNotes { get; set; }

            /// <summary>
            /// Gets or sets the tags of this mod.
            /// </summary>
            public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();

            /// <summary>
            /// Gets or sets the nice identifier of this mod.
            /// </summary>
            public string? Title { get; set; }

            /// <summary>
            /// Gets this mod's version.
            /// </summary>
            public Version Version { get; }

            /// <summary>
            /// Creates a new <see cref="Builder"/> instance with the given unique identifier and <see cref="IFileSystem"/>.
            /// </summary>
            /// <param name="id">The unique id for this mod.</param>
            /// <param name="version">The version for this mod.</param>
            /// <param name="fileSystem">The filesystem for this mod.</param>
            public Builder(string id, Version version, IFileSystem fileSystem)
            {
                Id = id;
                Version = version;
                FileSystem = fileSystem;
                Description = "Dynamic Mod";
            }

            /// <summary>
            /// Creates a new <see cref="Builder"/> instance with the given unique identifier and an empty <see cref="MemoryFileSystem"/>.
            /// </summary>
            /// <param name="id">The unique id for this mod.</param>
            /// <param name="version">The version for this mod.</param>
            public Builder(string id, Version version)
                : this(id, version, new MemoryFileSystem() { Name = $"{id} FileSystem" })
            { }

            /// <summary>
            /// Add an <see cref="IEarlyMonkey"/> type to the <see cref="Monkeys"/>.
            /// </summary>
            /// <typeparam name="TEarlyMonkey">The type of the early monkey to add.</typeparam>
            public void AddEarlyMonkey<TEarlyMonkey>() where TEarlyMonkey : EarlyMonkey<TEarlyMonkey>, new()
                => _earlyMonkeyTypes.Add(typeof(TEarlyMonkey));

            /// <summary>
            /// Adds the given <see cref="IEarlyMonkey"/>-implementing <see cref="Type"/>s to <see cref="EarlyMonkeys"/>.
            /// </summary>
            /// <param name="earlyMonkeyTypes">The types to add.</param>
            public void AddEarlyMonkeys(IEnumerable<Type> earlyMonkeyTypes)
                => _earlyMonkeyTypes.AddRange(earlyMonkeyTypes.Where(Monkey.EarlyMonkeyType.IsAssignableFrom));

            /// <summary>
            /// Add an <see cref="IMonkey"/> type to the <see cref="Monkeys"/>.
            /// </summary>
            /// <typeparam name="TMonkey">The type of the monkey to add.</typeparam>
            public void AddMonkey<TMonkey>() where TMonkey : Monkey<TMonkey>, new()
                => _monkeyTypes.Add(typeof(TMonkey));

            /// <summary>
            /// Adds the given <see cref="IMonkey"/>-implementing <see cref="Type"/>s to <see cref="Monkeys"/>.
            /// </summary>
            /// <param name="monkeyTypes">The types to add.</param>
            public void AddMonkeys(IEnumerable<Type> monkeyTypes)
                => _monkeyTypes.AddRange(monkeyTypes.Where(Monkey.MonkeyType.IsAssignableFrom));

            /// <summary>
            /// Constructs a <see cref="DynamicMod"/> from this builder, associating it with the given loader and running it immediately.
            /// Must only be used once.
            /// </summary>
            /// <param name="loader">The loader to add the mod to and run it with.</param>
            /// <returns>The <see cref="DynamicMod"/> constructed from this builder.</returns>
            /// <exception cref="InvalidOperationException">When this method is called more than once.</exception>
            public DynamicMod CreateAndRunFor(MonkeyLoader loader)
            {
                var mod = CreateFor(loader);

                loader.RunMod(mod);

                return mod;
            }

            /// <summary>
            /// Constructs a <see cref="DynamicMod"/> from this builder and associates it with the given loader.<br/>
            /// Must only be used once.
            /// </summary>
            /// <param name="loader">The loader to add the mod to.</param>
            /// <returns>The <see cref="DynamicMod"/> constructed from this builder.</returns>
            /// <exception cref="InvalidOperationException">When this method is called more than once.</exception>
            public DynamicMod CreateFor(MonkeyLoader loader)
            {
                if (_created)
                    throw new InvalidOperationException("Can only create the DynamicMod once!");

                _created = true;

                var mod = new DynamicMod(loader, this);
                loader.AddMod(mod);

                return mod;
            }
        }
    }
}