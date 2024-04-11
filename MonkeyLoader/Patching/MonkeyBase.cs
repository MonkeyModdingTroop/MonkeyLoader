using HarmonyLib;
using MonkeyLoader.Configuration;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonkeyLoader.Patching
{
    /// <summary>
    /// Abstract base for regular <see cref="Monkey{TMonkey}"/>s and <see cref="EarlyMonkey{TMonkey}"/>s.
    /// </summary>
    public abstract partial class MonkeyBase : IMonkey
    {
        /// <summary>
        /// The monkey's runtime type.
        /// </summary>
        protected readonly Type type;

        private readonly Lazy<IFeaturePatch[]> _featurePatches;
        private readonly Lazy<Harmony> _harmony;
        private Mod _mod = null!;
        /// <inheritdoc/>

        /// <inheritdoc/>
        public AssemblyName AssemblyName { get; }

        /// <inheritdoc/>
        public Config Config => Mod.Config;

        /// <summary>
        /// Gets whether this monkey's <see cref="Run">Run</see>() method failed when it was called.
        /// </summary>
        public bool Failed { get; protected set; }

        /// <inheritdoc/>
        public IEnumerable<IFeaturePatch> FeaturePatches => _featurePatches.Value.AsSafeEnumerable();

        /// <inheritdoc/>
        public Harmony Harmony => _harmony.Value;

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
            }
        }

        /// <remarks>
        /// <i>By Default</i>: The monkey's <see cref="type">type</see>'s Name.
        /// </remarks>
        /// <inheritdoc/>
        public virtual string Name => type.Name;

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

        internal MonkeyBase()
        {
            type = GetType();
            AssemblyName = new(type.Assembly.GetName().Name);

            _featurePatches = new Lazy<IFeaturePatch[]>(() =>
            {
                var featurePatches = GetFeaturePatches().ToArray();
                Array.Sort(featurePatches, FeaturePatch.DescendingComparer);

                return featurePatches;
            });

            _harmony = new(() => new Harmony($"{Mod.Title}/{Name}"));
        }

        /// <inheritdoc/>
        public int CompareTo(IMonkey other) => Monkey.AscendingComparer.Compare(this, other);

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
                Logger.Error(() => ex.Format("OnShutdown threw an Exception:"));
            }

            OnShutdownDone(applicationExiting);
            Logger.Debug(() => "OnShutdown done!");

            return !ShutdownFailed;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <i>Format:</i> <c>{<see cref="Mod">Mod</see>.<see cref="Mod.Title">Title</see>}/{<see cref="Name">Name</see>}</c>
        /// </remarks>
        public override string ToString() => $"{Mod.Title}/{Name}";

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
        protected abstract IEnumerable<IFeaturePatch> GetFeaturePatches();

        /// <summary>
        /// Lets this monkey cleanup and shutdown.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Removes all <see cref="Harmony"/> patches done
        /// using this Monkey's <see cref="MonkeyBase.Harmony">Harmony</see> instance,
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

        public event ShutdownHandler? ShutdownDone;

        /// <inheritdoc/>
        public event ShutdownHandler? ShuttingDown;
    }

    /// <inheritdoc/>
    /// <typeparam name="TMonkey">The type of the actual patcher.</typeparam>
    public abstract class MonkeyBase<TMonkey> : MonkeyBase where TMonkey : MonkeyBase<TMonkey>, new()
    {
        /// <summary>
        /// Gets the <see cref="Configuration.Config"/> that this patcher can use to load <see cref="ConfigSection"/>s.
        /// </summary>
        public new static Config Config => Instance.Config;

        /// <summary>
        /// Gets the <see cref="HarmonyLib.Harmony">Harmony</see> instance to be used by this patcher.
        /// </summary>
        public new static Harmony Harmony => Instance.Harmony;

        /// <summary>
        /// Gets the instance of this patcher.
        /// </summary>
        public static MonkeyBase Instance { get; } = new TMonkey();

        /// <summary>
        /// Gets the <see cref="Logging.Logger"/> that this patcher can use to log messages to game-specific channels.
        /// </summary>
        public new static Logger Logger => Instance.Logger;

        /// <summary>
        /// Gets the mod that this patcher is a part of.
        /// </summary>
        public new static Mod Mod => Instance.Mod;

        /// <summary>
        /// Allows creating only a single <typeparamref name="TMonkey"/> instance.
        /// </summary>
        internal MonkeyBase() : base()
        {
            if (Instance is not null)
                throw new InvalidOperationException("Can't create more than one patcher instance!");
        }
    }
}