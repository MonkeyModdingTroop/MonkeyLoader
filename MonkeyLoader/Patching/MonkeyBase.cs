using EnumerableToolkit;
using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MonkeyLoader.Patching
{
    /// <summary>
    /// Abstract base for regular <see cref="Monkey{TMonkey}"/>s and <see cref="EarlyMonkey{TMonkey}"/>s.
    /// </summary>
    public abstract class MonkeyBase : IMonkey
    {
        private readonly Lazy<IFeaturePatch[]> _featurePatches;

        private readonly Lazy<string> _fullId;

        private readonly Lazy<Harmony> _harmony;
        private Mod _mod = null!;

        /// <inheritdoc/>
        public AssemblyName AssemblyName { get; }

        /// <summary>
        /// Gets the names of the authors of this particular monkey,
        /// which may differ from the <see cref="Mod.Authors"/>.
        /// </summary>
        /// <remarks>
        /// <i>By default</i>: <c><see cref="Mod">Mod</see>.<see cref="Mod.Authors">Authors</see></c>.
        /// </remarks>
        public virtual IEnumerable<string> Authors => _mod.Authors;

        /// <inheritdoc/>
        /// <remarks>
        /// <i>By default</i>: <c>false</c>.
        /// </remarks>
        [MemberNotNullWhen(true, nameof(EnabledToggle), nameof(EnabledToggle))]
        public virtual bool CanBeDisabled => false;

        /// <inheritdoc/>
        public Config Config => Mod.Config;

        /// <remarks>
        /// <i>By default:</i> <see langword="null"/>.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string? Description => null;

        /// <inheritdoc/>
        public bool Enabled
        {
            get => !CanBeDisabled || EnabledToggle.GetValue();
            set
            {
                if (CanBeDisabled)
                {
                    EnabledToggle.SetValue(value, "SetMonkeyEnabled");
                    return;
                }

                if (!value)
                    throw new NotSupportedException("This monkey can't be disabled!");
            }
        }

        /// <inheritdoc/>
        public IDefiningConfigKey<bool>? EnabledToggle { get; private set; }

        /// <summary>
        /// Gets whether this monkey's <see cref="Run">Run</see>() method failed when it was called.
        /// </summary>
        public bool Failed { get; protected set; }

        /// <inheritdoc/>
        public IEnumerable<IFeaturePatch> FeaturePatches => _featurePatches.Value.AsSafeEnumerable();

        /// <summary>
        /// Gets fully unique identifier of this monkey.
        /// </summary>
        /// <remarks>
        /// Format:
        /// <c>$"{<see cref="Mod">Mod</see>.<see cref="Mod.Id">Id</see>}.{<see cref="Id">Id</see>}"</c>
        /// </remarks>
        public string FullId => _fullId.Value;

        /// <inheritdoc/>
        public Harmony Harmony => _harmony.Value;

        /// <inheritdoc/>
        public bool HasDescription => Description is not null;

        /// <summary>
        /// Gets the mod-unique identifier of this monkey.
        /// </summary>
        /// <remarks>
        /// <i>By Default</i>: This monkey's <see cref="object.GetType">type</see>'s <see cref="MemberInfo.Name">name</see>.
        /// </remarks>
        public virtual string Id => Type.Name;

        /// <inheritdoc/>
        public Logger Logger { get; private set; } = null!;

        /// <inheritdoc/>
        public Mod Mod
        {
            get => _mod;

            [MemberNotNull(nameof(_mod))]
            internal set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                if (ReferenceEquals(_mod, value))
                    return;

                if (_mod is not null)
                    throw new InvalidOperationException("Can't assign a different mod to a monkey!");

                _mod = value;
                Logger = new Logger(_mod.Logger, Name);

                if (!CanBeDisabled)
                    return;

                EnabledToggle = _mod.MonkeyToggles.GetToggle(this, OnComputeDefaultEnabledState);
                EnabledToggle.Changed += OnActiveStateChanged;
            }
        }

        /// <remarks>
        /// <i>By Default</i>: This monkey's <see cref="Id">Id</see>.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string Name => Id;

        Mod INestedIdentifiable<Mod>.Parent => _mod;

        IIdentifiable INestedIdentifiable.Parent => _mod;

        /// <summary>
        /// Gets whether this monkey's <see cref="Run">Run</see>() method has been called.
        /// </summary>
        public bool Ran { get; private protected set; } = false;

        /// <summary>
        /// Gets whether this monkey's <see cref="Shutdown">Shutdown</see>() failed when it was called.
        /// </summary>
        public bool ShutdownFailed { get; private set; } = false;

        /// <summary>
        /// Gets whether this monkey's <see cref="Shutdown">Shutdown</see>() method has been called.
        /// </summary>
        public bool ShutdownRan { get; private set; } = false;

        /// <inheritdoc/>
        public Type Type { get; }

        /// <summary>
        /// Initializes the monkey base.
        /// </summary>
        internal MonkeyBase()
        {
            Type = GetType();
            AssemblyName = new(Type.Assembly.GetName().Name);

            _featurePatches = new Lazy<IFeaturePatch[]>(() =>
            {
                var featurePatches = GetFeaturePatches().ToArray();
                Array.Sort(featurePatches, FeaturePatch.DescendingComparer);

                return featurePatches;
            });

            _fullId = new(() => $"{Mod.Id}.{Id}");
            _harmony = new(() => new Harmony(FullId));
        }

        /// <inheritdoc/>
        public int CompareTo(IMonkey other) => Monkey.AscendingComparer.Compare(this, other);

        /// <inheritdoc/>
        public bool HasAuthor(string name)
            => Authors.Any(author => author.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Runs this monkey to let it patch.<br/>
        /// Must only be called once.
        /// </summary>
        /// <inheritdoc/>
        public abstract bool Run();

        /// <summary>
        /// Lets this monkey cleanup and shutdown.<br/>
        /// Must only be called once.
        /// </summary>
        /// <inheritdoc/>
        public bool Shutdown(bool applicationExiting)
        {
            if (ShutdownRan)
                throw new InvalidOperationException("A monkey's Shutdown() method must only be called once!");

            ShutdownRan = true;

            Logger.Debug(() => "Running OnShutdown!");
            OnShuttingDown(applicationExiting);

            try
            {
                if (!OnShutdown(applicationExiting))
                {
                    ShutdownFailed = true;
                    Logger.Warn(() => "OnShutdown failed!");
                }
            }
            catch (Exception ex)
            {
                ShutdownFailed = true;
                Logger.Error(ex.LogFormat("OnShutdown threw an Exception:"));
            }

            OnShutdownDone(applicationExiting);
            Logger.Debug(() => "OnShutdown done!");

            return !ShutdownFailed;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <i>Format:</i> <c>{<see cref="Mod">Mod</see>.<see cref="Mod.Title">Title</see>}/{<see cref="Name">Name</see>}
        /// ({(<see cref="Ran">Ran</see> ? (<see cref="ShutdownRan">ShutdownRan</see> ? "Ended" : (<see cref="Enabled">Enabled</see> ? "Active" : "Inactive")) : "Pending")})</c>
        /// </remarks>
        public override string ToString() => $"{Mod.Title}/{Name} ({(Ran ? (ShutdownRan ? "Ended" : (Enabled ? "Active" : "Inactive")) : "Pending")})";

        internal static TMonkey GetInstance<TMonkey>(Type type, Mod mod) where TMonkey : IMonkey
        {
            // Could do more specific inheriting from Monkey<> check
            if (!typeof(TMonkey).IsAssignableFrom(type))
                throw new ArgumentException($"Given type [{type}] doesn't inherit from {typeof(TMonkey).FullName}!", nameof(type));

            var monkey = Traverse.Create(type).Property<MonkeyBase>("Instance").Value;
            monkey.Mod = mod;

            return (TMonkey)(object)monkey;
        }

        /// <summary>
        /// Gets the impacts this (pre-)patcher has on certain features.
        /// </summary>
        protected virtual IEnumerable<IFeaturePatch> GetFeaturePatches() => [];

        /// <summary>
        /// Lets this monkey compute the default value for its <see cref="EnabledToggle">EnabledToggle</see>.
        /// </summary>
        /// <remarks>
        /// This method may return different values depending on the current state of the application.<br/>
        /// <i>By default:</i> returns <c>true</c>.
        /// </remarks>
        /// <returns><c>true</c> if this monkey should be enabled by default; otherwise, <c>false</c>.</returns>
        protected virtual bool OnComputeDefaultEnabledState() => true;

        /// <summary>
        /// Lets this monkey react to being disabled at runtime.<br/>
        /// Will only ever be called when <see cref="CanBeDisabled">CanBeDisabled</see> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> does nothing.
        /// </remarks>
        protected virtual void OnDisabled()
        { }

        /// <summary>
        /// Lets this monkey react to being enabled at runtime.<br/>
        /// Will only ever be called when <see cref="CanBeDisabled">CanBeDisabled</see> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> does nothing.
        /// </remarks>
        protected virtual void OnEnabled()
        { }

        /// <summary>
        /// Lets this monkey cleanup and shutdown.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Removes all <see cref="HarmonyLib.Harmony"/> patches done
        /// using this Monkey's <see cref="Harmony">Harmony</see> instance,
        /// if not exiting, and returns <c>true</c>.
        /// </remarks>
        /// <param name="applicationExiting">Whether the shutdown was caused by the application exiting.</param>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        protected virtual bool OnShutdown(bool applicationExiting)
        {
            // Application Exit clears patches anyways
            if (!applicationExiting)
                Harmony.UnpatchAll(Harmony.Id);

            return true;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if <see cref="Ran"/> is <c>true</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">If <see cref="Ran"/> is <c>true</c>.</exception>
        protected void ThrowIfRan()
        {
            if (Ran)
                throw new InvalidOperationException("A monkey's Run() method must only be called once!");
        }

        private void OnActiveStateChanged(object sender, ConfigKeyChangedEventArgs<bool> configKeyChangedEventArgs)
        {
            if (configKeyChangedEventArgs.Label is ConfigKey.SetFromLoadEventLabel or ConfigKey.SetFromDefaultEventLabel)
                return;

            try
            {
                if (configKeyChangedEventArgs.NewValue)
                    OnEnabled();
                else
                    OnDisabled();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.LogFormat($"{(configKeyChangedEventArgs.NewValue ? nameof(OnEnabled) : nameof(OnDisabled))}() threw an Exception:"));
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

        /// <inheritdoc/>
        public event ShutdownHandler? ShutdownDone;

        /// <inheritdoc/>
        public event ShutdownHandler? ShuttingDown;
    }

    /// <inheritdoc/>
    /// <typeparam name="TMonkey">The type of the actual patcher.</typeparam>
    public abstract class MonkeyBase<TMonkey> : MonkeyBase where TMonkey : MonkeyBase<TMonkey>, new()
    {
        private static readonly MonkeyBase _instance = new TMonkey();

        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this patcher can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public new static Config Config => _instance.Config;

        /// <summary>
        /// Gets or sets whether this monkey should currently be active.
        /// </summary>
        /// <remarks>
        /// Can only be set to <c>false</c> if the monkey
        /// supports <see cref="MonkeyBase.CanBeDisabled">being disabled</see>.
        /// </remarks>
        public new static bool Enabled
        {
            get => _instance.Enabled;
            set => _instance.Enabled = value;
        }

        /// <summary>
        /// Gets the <see cref="HarmonyLib.Harmony">Harmony</see> instance to be used by this patcher.
        /// </summary>
        public new static Harmony Harmony => _instance.Harmony;

        /// <summary>
        /// Gets the instance of this patcher.
        /// </summary>
        public static TMonkey Instance => (TMonkey)_instance;

        /// <summary>
        /// Gets the <see cref="Logging.Logger"/> that this patcher can use to log messages to game-specific channels.
        /// </summary>
        public new static Logger Logger => _instance.Logger;

        /// <summary>
        /// Gets the mod that this patcher is a part of.
        /// </summary>
        public new static Mod Mod => _instance.Mod;

        /// <summary>
        /// Allows creating only a single <typeparamref name="TMonkey"/> instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the concrete Type isn't <typeparamref name="TMonkey"/>; or when there's already an <see cref="Instance">Instance</see>.</exception>
        internal MonkeyBase() : base()
        {
            if (GetType() != typeof(TMonkey))
                throw new InvalidOperationException("TMonkey must be the concrete Type being instantiated!");

            if (_instance is not null)
                throw new InvalidOperationException("Can't create more than one patcher instance!");
        }
    }
}