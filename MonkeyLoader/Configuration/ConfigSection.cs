using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents a section of a <see cref="Configuration.Config"/> for any <see cref="IConfigOwner"/>.
    /// </summary>
    /// <remarks>
    /// Use your mod's <see cref="Configuration.Config"/> instance to <see cref="Config.LoadSection{TSection}()">load sections</see>.
    /// </remarks>
    public abstract class ConfigSection : INestedIdentifiable<Config>, IIdentifiableOwner<ConfigSection, IDefiningConfigKey>
    {
        /// <summary>
        /// Stores the <see cref="IDefiningConfigKey"/>s tracked by this section.
        /// </summary>
        protected readonly HashSet<IDefiningConfigKey> keys = new();

        private readonly Lazy<string> _fullId;

        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this section is a part of.
        /// </summary>
        // Make the Compiler shut up about Config not being set - it gets set by the Config loading the section.
        public Config Config { get; internal set; } = null!;

        /// <summary>
        /// Gets a description of the config items found in this section.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the fully qualified unique identifier for this section.
        /// </summary>
        /// <remarks>
        /// Format:
        /// <c>$"{<see cref="Config">Config</see>.<see cref="Config.Owner">Owner</see>.<see cref="IIdentifiable.Id">Id</see>}.{<see cref="Id">Id</see>}"</c>
        /// </remarks>
        public string FullId => _fullId.Value;

        /// <summary>
        /// Gets whether there are any config keys with unsaved changes in this section.
        /// </summary>
        public bool HasChanges => keys.Any(key => key.HasChanges);

        /// <summary>
        /// Gets the mod-unique identifier of this section.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Gets whether only the owning mod should have access to this config item.
        /// </summary>
        /// <remarks>
        /// <i>By default</i>: <c>false</c>.
        /// </remarks>
        public virtual bool InternalAccessOnly => false;

        IEnumerable<IDefiningConfigKey> IIdentifiableOwner<IDefiningConfigKey>.Items => Keys;

        /// <summary>
        /// Gets all the config keys of this section in order of their <see cref="IPrioritizable.Priority">priority</see>.
        /// </summary>
        public IEnumerable<IDefiningConfigKey> Keys => keys.OrderByDescending(key => key.Priority);

        /// <summary>
        /// Gets the name for this section.
        /// </summary>
        public virtual string Name => Id;

        IIdentifiable INestedIdentifiable.Parent => Config;

        Config INestedIdentifiable<Config>.Parent => Config;

        /// <summary>
        /// Gets whether this config section is allowed to be saved.<br/>
        /// This can be <c>false</c> if something went wrong while loading it.
        /// </summary>
        public bool Saveable { get; internal set; } = true;

        /// <summary>
        /// Gets the semantic version for this config section.<br/>
        /// This is used to check if the defined and saved configs are compatible.
        /// </summary>
        public abstract Version Version { get; }

        /// <summary>
        /// Gets the way that an incompatible saved configuration should be treated.<br/>
        /// <see cref="IncompatibleConfigHandling.Error"/> by default
        /// </summary>
        protected virtual IncompatibleConfigHandling IncompatibilityHandling => IncompatibleConfigHandling.Error;

        /// <summary>
        /// Gets the logger of the config this section belongs to.
        /// </summary>
        private Logger Logger => Config.Logger;

        /// <summary>
        /// Creates a new config section instance.
        /// </summary>
        protected ConfigSection()
        {
            _fullId = new(() => $"{Config.FullId}.{Id}");
        }

        /// <summary>
        /// Checks if two <see cref="ConfigSection"/>s are unequal.
        /// </summary>
        /// <param name="left">The first section.</param>
        /// <param name="right">The second section.</param>
        /// <returns><c>true</c> if they're considered unequal.</returns>
        public static bool operator !=(ConfigSection? left, ConfigSection? right)
            => !(left == right);

        /// <summary>
        /// Checks if two <see cref="ConfigSection"/>s are equal.
        /// </summary>
        /// <param name="left">The first section.</param>
        /// <param name="right">The second section.</param>
        /// <returns><c>true</c> if they're considered equal.</returns>
        public static bool operator ==(ConfigSection? left, ConfigSection? right)
            => left?.FullId == right?.FullId;

        /// <summary>
        /// Checks if the given object can be considered equal to this one.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns><c>true</c> if the other object is considered equal.</returns>
        public override bool Equals(object obj)
            => obj is ConfigSection section && section == this;

        /// <summary>
        /// Gets the <see cref="IDefiningConfigKey"/> defined in this config section,
        /// which matches the given <paramref name="templateKey"/>.
        /// </summary>
        /// <param name="templateKey">The config item to search for.</param>
        /// <returns>The matching item defined in this config section.</returns>
        /// <exception cref="KeyNotFoundException">When no matching item is defined in this config section.</exception>
        public IDefiningConfigKey GetDefinedKey(IConfigKey templateKey)
        {
            if (!TryGetDefinedKey(templateKey, out var definingKey))
                ThrowKeyNotFound(templateKey);

            return definingKey;
        }

        /// <summary>
        /// Gets the <see cref="IDefiningConfigKey{T}"/> defined in this config section,
        /// which matches the given <paramref name="templateKey"/>.
        /// </summary>
        /// <param name="templateKey">The config item to search for.</param>
        /// <returns>The matching item defined in this config section.</returns>
        /// <exception cref="KeyNotFoundException">When no matching item is defined in this config section.</exception>
        public IDefiningConfigKey<T> GetDefinedKey<T>(IDefiningConfigKey<T> templateKey)
        {
            if (!TryGetDefinedKey(templateKey, out var definingKey))
                ThrowKeyNotFound(templateKey);

            return definingKey;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Invokes this config section's <see cref="ItemChanged">ItemChanged</see> event,
        /// and passes the invocation on to the <see cref="Config"/> and <see cref="MonkeyLoader"/> it belongs to.
        /// </summary>
        /// <param name="configKeyChangedEventArgs">The data for the event.</param>
        public void OnItemChanged(IConfigKeyChangedEventArgs configKeyChangedEventArgs)
        {
            try
            {
                ItemChanged?.TryInvokeAll(this, configKeyChangedEventArgs);
            }
            catch (AggregateException ex)
            {
                Logger.Error(ex.LogFormat($"Some ConfigSection.{nameof(ItemChanged)} event subscriber(s) threw an exception:"));
            }

            Config.OnItemChanged(configKeyChangedEventArgs);
        }

        /// <summary>
        /// Determines if this config section contains an item matching the <paramref name="typedTemplateKey"/>
        /// and returns the optional match as <paramref name="definingKey"/>.
        /// </summary>
        /// <param name="typedTemplateKey">The config item to search for.</param>
        /// <param name="definingKey">The optional match for the searched item. Will also contain a match not from this config section.</param>
        /// <returns><c>true</c> if this config section contains the matching item; otherwise, <c>false</c>.</returns>
        public bool TryGetDefinedKey<T>(ITypedConfigKey<T> typedTemplateKey, [NotNullWhen(true)] out IDefiningConfigKey<T>? definingKey)
            => Config.TryGetDefiningKey(typedTemplateKey, out definingKey) && keys.Contains(definingKey);

        /// <summary>
        /// Determines if this config section contains an item matching the <paramref name="templateKey"/>
        /// and returns the optional match as <paramref name="definingKey"/>.
        /// </summary>
        /// <param name="templateKey">The config item to search for.</param>
        /// <param name="definingKey">The optional match for the searched item. Will also contain a match not from this config section.</param>
        /// <returns><c>true</c> if this config section contains the matching item; otherwise, <c>false</c>.</returns>
        public bool TryGetDefinedKey(IConfigKey templateKey, [NotNullWhen(true)] out IDefiningConfigKey? definingKey)
            => Config.TryGetDefiningKey(templateKey.AsUntyped, out definingKey) && keys.Contains(definingKey);

        internal void InitializeKeys()
        {
            var keysToAdd = GetConfigKeys().ToArray();
            keys.AddRange(keysToAdd);

            foreach (var key in keysToAdd)
                key.Section = this;

            foreach (var key in keysToAdd)
                Config.RegisterConfigKey(key);
        }

        internal void Load(JObject source, JsonSerializer jsonSerializer)
        {
            if (source.Count > 0)
            {
                Version serializedVersion;

                try
                {
                    serializedVersion = new Version((string)source[nameof(Version)]!);
                }
                catch (Exception ex)
                {
                    // I know not what exceptions the JSON library will throw, but they must be contained
                    Saveable = false;
                    throw new ConfigLoadException($"Error loading version for section [{Id}]!", ex);
                }

                ValidateCompatibility(serializedVersion);
            }

            OnLoad(source, jsonSerializer);
        }

        internal void ResetHasChanges()
        {
            foreach (var key in keys)
                key.HasChanges = false;
        }

        internal JObject? Save(JsonSerializer jsonSerializer)
        {
            if (!Saveable)
                return null;

            var result = new JObject();
            result["Version"] = Version.ToString();

            // Any exceptions get handled by the Config.Save method
            OnSave(result, jsonSerializer);

            return result;
        }

        /// <summary>
        /// Deserializes the given <paramref name="key"/> from the <paramref name="source"/>
        /// with the <paramref name="jsonSerializer"/>, if there is a value.
        /// </summary>
        /// <param name="key">The key to deserialize.</param>
        /// <param name="source">The <see cref="JObject"/> being deserialized from.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> to deserialize objects with.</param>
        protected void DeserializeKey(IDefiningConfigKey key, JObject source, JsonSerializer jsonSerializer)
        {
            if (source[key.Id] is not JToken token)
                return;

            var value = token.ToObject(key.ValueType, jsonSerializer);
            key.SetValue(value, ConfigKey.SetFromLoadEventLabel);
            key.HasChanges = false;
        }

        /// <summary>
        /// Gets the <see cref="IDefiningConfigKey"/>s from all fields of this <see cref="ConfigSection"/> which have a <see cref="Type"/>
        /// derived from <see cref="IDefiningConfigKey"/> and don't have a <see cref="IgnoreConfigKeyAttribute"/>.
        /// </summary>
        /// <returns>The automatically tracked <see cref="IDefiningConfigKey"/>s.</returns>
        protected IEnumerable<IDefiningConfigKey> GetAutoConfigKeys()
        {
            var configKeyType = typeof(IDefiningConfigKey);

            return GetType().GetFields(AccessTools.all | BindingFlags.FlattenHierarchy)
                .Where(field => configKeyType.IsAssignableFrom(field.FieldType)
                             && field.GetCustomAttribute<IgnoreConfigKeyAttribute>() is null)
                .Select(field => field.GetValue(this))
                .Cast<IDefiningConfigKey>();
        }

        /// <summary>
        /// Gets all <see cref="IDefiningConfigKey"/>s which should be tracked for this <see cref="ConfigSection"/>.
        /// </summary>
        /// <remarks>
        /// <i>By default</i>: Calls <see cref="GetAutoConfigKeys"/>.
        /// </remarks>
        /// <returns>All <see cref="IDefiningConfigKey"/>s to track.</returns>
        protected virtual IEnumerable<IDefiningConfigKey> GetConfigKeys() => GetAutoConfigKeys();

        /// <summary>
        /// Deserializes all <see cref="Keys">keys</see> of this
        /// <see cref="ConfigSection"/> from the <paramref name="source"/> <see cref="JObject"/>.
        /// </summary>
        /// <remarks>
        /// <i>By default</i>: Deserializes all <see cref="Keys">keys</see> from the <paramref name="source"/> with the <paramref name="jsonSerializer"/>.
        /// </remarks>
        /// <param name="source">The <see cref="JObject"/> being deserialized from. May be empty for when file didn't have it yet.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> to deserialize objects with.</param>
        /// <exception cref="ConfigLoadException">When the value for a key fails to deserialize.</exception>
        protected virtual void OnLoad(JObject source, JsonSerializer jsonSerializer)
        {
            foreach (var key in keys)
            {
                try
                {
                    DeserializeKey(key, source, jsonSerializer);
                }
                catch (Exception ex)
                {
                    // I know not what exceptions the JSON library will throw, but they must be contained
                    Saveable = false;
                    throw new ConfigLoadException($"Error loading key [{key.Id}] of type [{key.ValueType}] in section [{Id}]!", ex);
                }
            }
        }

        /// <summary>
        /// Serializes all <see cref="Keys">keys</see> of this
        /// <see cref="ConfigSection"/> to the <paramref name="result"/> <see cref="JObject"/>.
        /// </summary>
        /// <remarks>
        /// <i>By default</i>: Serializes all <see cref="Keys">keys</see> to the <paramref name="result"/> with the <paramref name="jsonSerializer"/>.
        /// </remarks>
        /// <param name="result">The <see cref="JObject"/> being serialized to.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> to serialize objects with.</param>
        protected virtual void OnSave(JObject result, JsonSerializer jsonSerializer)
        {
            foreach (var key in keys)
                SerializeKey(key, result, jsonSerializer);
        }

        /// <summary>
        /// Serializes the given <paramref name="key"/> to the <paramref name="result"/>
        /// with the <paramref name="jsonSerializer"/>, if the value can be gotten.
        /// </summary>
        /// <param name="key">The key to serialize.</param>
        /// <param name="result">The <see cref="JObject"/> being serialized to.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> to serialize objects with.</param>
        protected void SerializeKey(IDefiningConfigKey key, JObject result, JsonSerializer jsonSerializer)
        {
            if (!key.TryGetValue(out var value))
                return;

            // I don't need to typecheck this as there's no way to sneak a bad type past my Set() API
            result[key.Id] = value == null ? null : JToken.FromObject(value, jsonSerializer);
        }

        /// <summary>
        /// Throws a <see cref="KeyNotFoundException"/> for the given <paramref name="key"/> in this config section.
        /// </summary>
        /// <param name="key">The key that wasn't found.</param>
        /// <exception cref="KeyNotFoundException">Always.</exception>
        [DoesNotReturn]
        protected void ThrowKeyNotFound(IConfigKey key)
            => throw new KeyNotFoundException($"Key [{key.Id}] not found in this config section!");

        private static bool AreVersionsCompatible(Version serializedVersion, Version currentVersion)
        {
            if (serializedVersion.Major != currentVersion.Major)
            {
                // major version differences are hard incompatible
                return false;
            }

            if (serializedVersion.Minor > currentVersion.Minor)
            {
                // if serialized config has a newer minor version than us
                // in other words, someone downgraded the mod but not the config
                // then we cannot load the config
                return false;
            }

            // none of the checks failed!
            return true;
        }

        private void ValidateCompatibility(Version serializedVersion)
        {
            if (!AreVersionsCompatible(serializedVersion, Version))
            {
                switch (IncompatibilityHandling)
                {
                    case IncompatibleConfigHandling.Clobber:
                        Logger.Warn(() => $"Saved section [{Id}] version [{serializedVersion}] is incompatible with mod's version [{Version}]. Clobbering old config and starting fresh.");
                        return;

                    case IncompatibleConfigHandling.ForceLoad:
                        // continue processing
                        break;

                    case IncompatibleConfigHandling.Error: // fall through to default
                    default:
                        Saveable = false;
                        throw new ConfigLoadException($"Saved section [{Id}] version [{serializedVersion}] is incompatible with mod's version [{Version}]!");
                }
            }
        }

        /// <summary>
        /// Called when the value of one of this config's items gets changed.
        /// </summary>
        public event ConfigKeyChangedEventHandler? ItemChanged;
    }
}