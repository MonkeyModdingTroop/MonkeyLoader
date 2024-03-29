using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Configuration
{
    /// <summary>
    /// Represents a section of a <see cref="Config"/> for any <see cref="IConfigOwner"/>,
    /// which additional config items can be added to dynamically.
    /// </summary>
    /// <inheritdoc/>
    public abstract class ExpandoConfigSection : ConfigSection
    {
        private JsonSerializer? _jsonSerializer;
        private JObject? _source;

        /// <summary>
        /// Gets the <see cref="Newtonsoft.Json.JsonSerializer"/> that added <see cref="DefiningConfigKey{T}"/>s will be attempted to be deserialized with.
        /// </summary>
        protected JsonSerializer JsonSerializer => _jsonSerializer ??
            throw new InvalidOperationException($"Tried to access {nameof(JsonSerializer)} before the section was loaded!");

        /// <summary>
        /// Gets the source <see cref="JObject"/> that added <see cref="DefiningConfigKey{T}"/>s will be attempted to be deserialized from.
        /// </summary>
        protected JObject Source => _source ??
            throw new InvalidOperationException($"Tried to access {Source} before the section was loaded!");

        /// <summary>
        /// Creates a new <see cref="DefiningConfigKey{T}"/> in this config section.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="templateKey">The typed name-only config item to match.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this config item. Otherwise everything will be accepted.</param>
        /// <returns>The created defining config item.</returns>
        /// <exception cref="InvalidOperationException">When there is already a config item that matches the name defined.</exception>
        public IDefiningConfigKey<T> CreateDefiningKey<T>(ITypedConfigKey<T> templateKey, string? description = null,
                    Func<T>? computeDefault = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
        {
            if (Config.TryGetDefiningKey(templateKey, out _))
                throw new InvalidOperationException($"Key matching the template's Name [{templateKey.Name}] already exists!");

            return AddDefiningKey(templateKey.Name, description, computeDefault, internalAccessOnly, valueValidator);
        }

        /// <summary>
        /// Tries to get a config item defined by this config section,
        /// which matches the <paramref name="templateKey"/>'s name and type.
        /// Creates a new <see cref="DefiningConfigKey{T}"/> if no item with that name exists yet.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="templateKey">The typed name-only config item to match.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this config item. Otherwise everything will be accepted.</param>
        /// <returns>The found or created defining config item.</returns>
        /// <exception cref="InvalidOperationException">When there is already a config item that matches the name defined by another section, or with the wrong type.</exception>
        public IDefiningConfigKey<T> GetOrCreateDefiningKey<T>(ITypedConfigKey<T> templateKey,
            string? description = null, Func<T>? computeDefault = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
        {
            if (TryGetDefinedKey(templateKey.AsUntyped, out var definingKey))
                return definingKey as IDefiningConfigKey<T> ?? throw new InvalidOperationException($"Key matching the template's Name [{templateKey.Name}] exists, but has the wrong type!");

            if (definingKey is not null)
                throw new InvalidOperationException($"Key matching the template's Name [{templateKey.Name}] exists, but wasn't defined by this config section!");

            return AddDefiningKey(templateKey.Name, description, computeDefault, internalAccessOnly, valueValidator);
        }

        /// <summary>
        /// Tries to get a config item defined by this config section,
        /// which matches the <paramref name="typedTemplateKey"/>'s name and type.
        /// Creates a new <see cref="DefiningConfigKey{T}"/> if no item with that name exists yet.<br/>
        /// The found or created item is returned as <paramref name="typedDefiningKey"/>.
        /// </summary>
        /// <typeparam name="T">The type of the config item's value.</typeparam>
        /// <param name="typedTemplateKey">The typed name-only config item to match.</param>
        /// <param name="typedDefiningKey">The optional output of the found or created defining config item. Will also contain a match not from this config section.</param>
        /// <param name="description">The human-readable description of this config item.</param>
        /// <param name="computeDefault">The function that computes a default value for this key. Otherwise <c>default(<typeparamref name="T"/>)</c> will be used.</param>
        /// <param name="internalAccessOnly">If <c>true</c>, only the owning mod should have access to this config item.</param>
        /// <param name="valueValidator">The function that checks if the given value is valid for this config item. Otherwise everything will be accepted.</param>
        /// <returns><c>true</c> if a config item matching the <paramref name="typedTemplateKey"/> was found or created; otherwise, <c>false</c>.</returns>
        public bool TryGetOrCreateDefiningKey<T>(ITypedConfigKey<T> typedTemplateKey, [NotNullWhen(true)] out IDefiningConfigKey<T>? typedDefiningKey,
            string? description = null, Func<T>? computeDefault = null, bool internalAccessOnly = false, Predicate<T?>? valueValidator = null)
        {
            if (TryGetDefinedKey(typedTemplateKey.AsUntyped, out var definingKey))
            {
                typedDefiningKey = definingKey as IDefiningConfigKey<T>;
                return typedDefiningKey is not null;
            }

            // Key exists, but isn't defined by this config section
            if (definingKey is not null)
            {
                typedDefiningKey = definingKey as IDefiningConfigKey<T>;
                return false;
            }

            typedDefiningKey = AddDefiningKey(typedTemplateKey.Name, description, computeDefault, internalAccessOnly, valueValidator);
            return true;
        }

        /// <inheritdoc/>
        protected override void OnLoad(JObject source, JsonSerializer jsonSerializer)
        {
            _source = source;
            _jsonSerializer = jsonSerializer;

            base.OnLoad(source, jsonSerializer);
        }

        private IDefiningConfigKey<T> AddDefiningKey<T>(string name, string? description, Func<T>? computeDefault, bool internalAccessOnly, Predicate<T?>? valueValidator)
        {
            var definingKey = new DefiningConfigKey<T>(name, description, computeDefault, internalAccessOnly, valueValidator);
            definingKey.Section = this;

            keys.Add(definingKey);
            Config.RegisterConfigKey(definingKey);

            try
            {
                DeserializeKey(definingKey, Source, JsonSerializer);
            }
            catch (Exception ex)
            {
                // I know not what exceptions the JSON library will throw, but they must be contained
                // Saveable = false;
                Config.Logger.Error(() => ex.Format($"Error loading expando key [{name}] of type [{definingKey.ValueType}] in section [{Name}]!"));
            }

            return definingKey;
        }
    }
}