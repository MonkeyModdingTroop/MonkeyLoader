﻿using MonkeyLoader.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.NuGet
{
    public sealed class NuGetConfigSection : ConfigSection
    {
        public readonly DefiningConfigKey<bool> EnableLoadingLibsKey = new("EnableLoadingLibs", "Allows checking NuGet feeds to load mod's library dependencies.", () => true);
        public readonly DefiningConfigKey<bool> EnableLoadingModsKey = new("EnableLoadingMods", "Allows checking NuGet feeds to load mod's other-mod dependencies.", () => true);
        public readonly DefiningConfigKey<List<NuGetSource>> NuGetGamePackSourcesKey = new("NuGetGamePackSources", "NuGet feeds to check for game packs.", () => new());
        public readonly DefiningConfigKey<List<NuGetSource>> NuGetLibSourcesKey = new("NuGetLibSources", "NuGet feeds to check for libraries.", () => new() { new("Official NuGet Feed", new("https://api.nuget.org/v3/index.json")) });

        public readonly DefiningConfigKey<List<NuGetSource>> NuGetModSourcesKey = new("NuGetModSources", "NuGet feeds to check for mods.", () => new());

        /// <inheritdoc/>
        public override string Description { get; } = "Contains definitions for how to use which NuGet feeds.";

        /// <inheritdoc/>
        public override string Id { get; } = "NuGet";

        /// <summary>
        /// Gets whether checking NuGet feeds to load mod's library dependencies is enabled.
        /// </summary>
        public bool LoadingLibsEnabled
        {
            get => Config.GetValue(EnableLoadingLibsKey);
            set => Config.SetValue(EnableLoadingLibsKey, value);
        }

        /// <summary>
        /// Gets whether checking NuGet feeds to load mod's other-mod dependencies is enabled.
        /// </summary>
        public bool LoadingModsEnabled
        {
            get => Config.GetValue(EnableLoadingModsKey);
            set => Config.SetValue(EnableLoadingModsKey, value);
        }

        /// <summary>
        /// Gets the NuGet feeds to check for game packs.
        /// </summary>
        public List<NuGetSource> NuGetGamePackSources
        {
            get => Config.GetValue(NuGetGamePackSourcesKey);
            set => Config.SetValue(NuGetGamePackSourcesKey, value);
        }

        /// <summary>
        /// Gets the NuGet feeds to check for libraries.
        /// </summary>
        public List<NuGetSource> NuGetLibSources
        {
            get => Config.GetValue(NuGetLibSourcesKey);
            set => Config.SetValue(NuGetLibSourcesKey, value);
        }

        /// <summary>
        /// Gets the NuGet feeds to check for mods.
        /// </summary>
        public List<NuGetSource> NuGetModSources
        {
            get => Config.GetValue(NuGetModSourcesKey);
            set => Config.SetValue(NuGetModSourcesKey, value);
        }

        /// <inheritdoc/>
        public override int Priority => 10;

        /// <inheritdoc/>
        public override Version Version { get; } = new Version(1, 0);
    }
}