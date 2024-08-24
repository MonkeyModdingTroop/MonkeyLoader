// Adapted from the NeosModLoader project.

using EnumerableToolkit;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// The configuration for a mod. Each mod has exactly one configuration.<br/>
    /// The configuration object will never be reassigned once initialized.
    /// </summary>
    public sealed class Config : INestedIdentifiable<IConfigOwner>,
        IIdentifiableOwner<Config, ConfigSection>, INestedIdentifiableOwner<IDefiningConfigKey>
    {
        private const string OwnerKey = "Owner";
        private const string SectionsKey = "Sections";

        // this is a ridiculous hack because HashSet.TryGetValue doesn't exist in .NET 4.6.2
        private readonly Dictionary<IConfigKey, IDefiningConfigKey> _configurationItemDefinitionsSelfMap = new(ConfigKey.EqualityComparer);

        private readonly JObject _loadedConfig;

        private readonly HashSet<ConfigSection> _sections = new();

        /// <summary>
        /// Gets the config keys defined in this configuration.
        /// </summary>
        public IEnumerable<IDefiningConfigKey> ConfigurationItemDefinitions => _configurationItemDefinitionsSelfMap.Values.AsSafeEnumerable();

        /// <inheritdoc/>
        public string FullId { get; }

        string IIdentifiable.Id => "Config";

        IEnumerable<IDefiningConfigKey> INestedIdentifiableOwner<IDefiningConfigKey>.Items => ConfigurationItemDefinitions;

        IEnumerable<ConfigSection> IIdentifiableOwner<ConfigSection>.Items => Sections;

        /// <summary>
        /// Gets the logger used by this config.
        /// </summary>
        public Logger Logger { get; }

        /// <summary>
        /// Gets the mod that owns this config.
        /// </summary>
        public IConfigOwner Owner { get; }

        IConfigOwner INestedIdentifiable<IConfigOwner>.Parent => Owner;

        IIdentifiable INestedIdentifiable.Parent => Owner;

        /// <summary>
        /// Gets all loaded sections of this config.
        /// </summary>
        public IEnumerable<ConfigSection> Sections => _sections.AsSafeEnumerable();

        /// <summary>
        /// Gets or sets a configuration value for the given key,
        /// throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// May throw an <see cref="ArgumentException"/> if the value is not valid for it.
        /// </summary>
        /// <remarks>
        /// This shorthand does not exist for <see cref="GetValue{T}"/>, because generic indexers aren't supported.
        /// </remarks>
        /// <param name="key">The key to get or set the value for.</param>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        /// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
        public object? this[IConfigKey key]
        {
            get => GetValue(key);
            set => SetValue(key, value);
        }

        internal Config(IConfigOwner owner)
        {
            Owner = owner;
            FullId = $"{Owner.FullId}.Config";
            Logger = new Logger(owner.Logger, "Config");

            _loadedConfig = LoadConfig();
            if (_loadedConfig[OwnerKey]?.ToObject<string>() != Owner.Id)
                throw new ConfigLoadException("Config malformed! Recorded owner must match the loading owner!");

            if (_loadedConfig[SectionsKey] is not JObject)
            {
                Logger.Warn(() => "Could not find \"Sections\" object - created it!");
                _loadedConfig[SectionsKey] = new JObject();
            }
        }

        /// <summary>
        /// Gets the configuration value for the given key,
        /// throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public object? GetValue(IConfigKey key)
        {
            if (!TryGetValue(key, out var value))
                ThrowKeyNotFound(key);

            return value;
        }

        /// <summary>
        /// Gets the configuration value for the given key,
        /// throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the key's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <returns>The value for the key.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public T GetValue<T>(ITypedConfigKey<T> key)
        {
            if (!TryGetValue(key, out var value))
                ThrowKeyNotFound(key);

            return value!;
        }

        /// <summary>
        /// Checks if the given key is defined in this config.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key is defined.</returns>
        public bool IsKeyDefined(IConfigKey key) => TryGetDefiningKey(key, out _);

        /// <summary>
        /// Loads a section with a parameterless constructor based on its type.<br/>
        /// Every section can only be loaded once.
        /// </summary>
        /// <typeparam name="TSection">The type of the section to load.</typeparam>
        /// <returns>The loaded section.</returns>
        /// <exception cref="ConfigLoadException">If section has already been loaded, or something goes wrong while loading.</exception>
        public TSection LoadSection<TSection>() where TSection : ConfigSection, new()
            => LoadSection(new TSection());

        /// <summary>
        /// Loads the given section.<br/>
        /// Every section can only be loaded once.
        /// </summary>
        /// <typeparam name="TSection">The type of the section to load.</typeparam>
        /// <returns>The loaded section.</returns>
        /// <exception cref="ConfigLoadException">If section has already been loaded, or something goes wrong while loading.</exception>
        public TSection LoadSection<TSection>(TSection section) where TSection : ConfigSection
        {
            if (_sections.Contains(section))
                throw new ConfigLoadException($"Attempted to load section [{section.Id}] twice!");

            section.Config = this;
            _sections.Add(section);

            section.InitializeKeys();

            if (_loadedConfig[SectionsKey]![section.Id] is not JObject)
            {
                _loadedConfig[SectionsKey]![section.Id] = new JObject();
                Logger.Warn(() => $"Section [{section.Id}] didn't appear in the loaded config - using defaults!");
            }

            section.Load((JObject)_loadedConfig[SectionsKey]![section.Id]!, Owner.Loader.JsonSerializer);

            return section;
        }

        /// <summary>
        /// Removes the value of all of this configuration's sections' keys and resets them to their default.
        /// </summary>
        public void Reset()
        {
            Logger.Warn(() => $"Resetting config: {FullId}");

            foreach (var section in Sections)
                section.Reset();
        }

        /// <summary>
        /// Persists this configuration to disk.
        /// </summary>
        public void Save()
        {
            if (!NeedsToSave())
            {
                Logger.Info(() => "Skipping save - no config keys with a set value and changes!");
                return;
            }

            var successfulSections = new List<ConfigSection>();
            var sectionsJson = (JObject)_loadedConfig[SectionsKey]!;
            var stopwatch = Stopwatch.StartNew();

            lock (_loadedConfig)
            {
                foreach (var section in _sections)
                {
                    try
                    {
                        var sectionJson = section.Save(Owner.Loader.JsonSerializer);

                        if (sectionJson is null)
                            continue;

                        successfulSections.Add(section);
                        sectionsJson[section.Id] = sectionJson;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.LogFormat($"Exception while serializing section [{section.Id}] - skipping it!"));
                    }
                }

                try
                {
                    using var file = File.OpenWrite(Owner.ConfigPath);
                    using var streamWriter = new StreamWriter(file);
                    using var jsonTextWriter = new JsonTextWriter(streamWriter);
                    jsonTextWriter.Formatting = Formatting.Indented;
                    _loadedConfig.WriteTo(jsonTextWriter);

                    // I actually cannot believe I have to truncate the file myself
                    file.SetLength(file.Position);
                    jsonTextWriter.Flush();

                    Logger.Info(() => $"Saved config in {stopwatch.ElapsedMilliseconds}ms!");

                    foreach (var section in successfulSections)
                        section.ResetHasChanges();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.LogFormat($"Exception while saving config!"));
                }
            }
        }

        /// <summary>
        /// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
        /// or an <see cref="ArgumentException"/> if the value is not valid for it.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="eventLabel">A custom label you may assign to this change event.</param>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        /// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
        public void SetValue<T>(ITypedConfigKey<T> key, T value, string? eventLabel = null)
        {
            if (!TryGetDefiningKey(key, out IDefiningConfigKey? definingKey))
                ThrowKeyNotFound(key);

            ((IDefiningConfigKey<T>)definingKey).SetValue(value, eventLabel);
        }

        /// <summary>
        /// Sets a configuration value for the given key, throwing a <see cref="KeyNotFoundException"/> if the key is not found
        /// or an <see cref="ArgumentException"/> if the value is not valid for it.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="eventLabel">A custom label you may assign to this change event.</param>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        /// <exception cref="ArgumentException">The new value is not valid for the given key.</exception>
        public void SetValue(IConfigKey key, object? value, string? eventLabel = null)
        {
            if (!TryGetDefiningKey(key, out IDefiningConfigKey? definingKey))
                ThrowKeyNotFound(key);

            definingKey.SetValue(value, eventLabel);
        }

        /// <summary>
        /// Tries to get the defining key in this config for the given key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="definingKey">The defining key in this config when this returns <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the key is defined in this config.</returns>
        public bool TryGetDefiningKey(IConfigKey key, [NotNullWhen(true)] out IDefiningConfigKey? definingKey)
        {
            if (_configurationItemDefinitionsSelfMap.TryGetValue(key, out definingKey))
                return true;

            // not a real key
            definingKey = null;
            return false;
        }

        /// <summary>
        /// Tries to get the defining key in this config for the given key.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="key">The key to check.</param>
        /// <param name="definingKey">The defining key in this config when this returns <c>true</c>, otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the key is defined in this config.</returns>
        public bool TryGetDefiningKey<T>(ITypedConfigKey<T> key, [NotNullWhen(true)] out IDefiningConfigKey<T>? definingKey)
        {
            if (_configurationItemDefinitionsSelfMap.TryGetValue(key, out var untypedDefiningKey))
            {
                definingKey = (IDefiningConfigKey<T>)untypedDefiningKey;
                return true;
            }

            // not a real key
            definingKey = null;
            return false;
        }

        /// <summary>
        /// Tries to get a value, returning <c>null</c> if the key is not found.
        /// </summary>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
        /// <returns><c>true</c> if the value was read successfully.</returns>
        public bool TryGetValue(IConfigKey key, out object? value)
        {
            if (TryGetDefiningKey(key, out var definingKey) && definingKey.TryGetValue(out value))
                return true;

            // not in definition or not set
            value = null;
            return false;
        }

        /// <summary>
        /// Tries to get a value, returning <c>default(<typeparamref name="T"/>)</c> if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="key">The key to get the value for.</param>
        /// <param name="value">The value if the return value is <c>true</c>, or <c>default</c> if <c>false</c>.</param>
        /// <returns><c>true</c> if the value was read successfully.</returns>
        public bool TryGetValue<T>(ITypedConfigKey<T> key, out T? value)
        {
            if (TryGetDefiningKey(key, out var definingKey) && definingKey.TryGetValue(out value))
                return true;

            // not in definition or not set
            value = default;
            return false;
        }

        /// <summary>
        /// Removes a key's value, throwing a <see cref="KeyNotFoundException"/> if the key is not found.
        /// </summary>
        /// <param name="key">The key to remove the value for.</param>
        /// <returns><c>true</c> if a value was successfully found and removed, <c>false</c> if there was no value to remove.</returns>
        /// <exception cref="KeyNotFoundException">The given key does not exist in the configuration.</exception>
        public bool Unset(IConfigKey key)
        {
            if (!TryGetDefiningKey(key, out var definingKey))
                ThrowKeyNotFound(key);

            return definingKey.Unset();
        }

        internal void OnItemChanged(IConfigKeyChangedEventArgs configKeyChangedEventArgs)
        {
            try
            {
                ItemChanged?.TryInvokeAll(this, configKeyChangedEventArgs);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex.LogFormat($"Some Config.{nameof(ItemChanged)} event subscriber(s) threw an exception:"));
            }

            Owner.Loader.OnAnyConfigChanged(configKeyChangedEventArgs);
        }

        internal void RegisterConfigKey(IDefiningConfigKey definingKey)
        {
            if (_configurationItemDefinitionsSelfMap.ContainsKey(definingKey.AsUntyped))
            {
                Logger.Error(() => $"Tried to load duplicate key id [{definingKey.Id}] from section [{definingKey.FullId}]!");
                definingKey.Section.Saveable = false;
            }
            else
            {
                _configurationItemDefinitionsSelfMap.Add(definingKey, definingKey);
            }
        }

        private JObject LoadConfig()
        {
            if (File.Exists(Owner.ConfigPath))
            {
                try
                {
                    using var file = File.OpenText(Owner.ConfigPath);
                    using var reader = new JsonTextReader(file);

                    return JObject.Load(reader);
                }
                catch (Exception ex)
                {
                    // I know not what exceptions the JSON library will throw, but they must be contained
                    throw new ConfigLoadException($"Error loading config!", ex);
                }
            }

            return new JObject()
            {
                [OwnerKey] = Path.GetFileNameWithoutExtension(Owner.ConfigPath),
                [SectionsKey] = new JObject()
            };
        }

        private bool NeedsToSave() => _configurationItemDefinitionsSelfMap.Values.Any(key => key.HasValue && key.HasChanges);

        [DoesNotReturn]
        private void ThrowKeyNotFound(IConfigKey key)
            => throw new KeyNotFoundException($"Key [{key.Id}] not found in this config!");

        /// <summary>
        /// Called when the value of one of this config's items gets changed.
        /// </summary>
        public event ConfigKeyChangedEventHandler? ItemChanged;
    }
}