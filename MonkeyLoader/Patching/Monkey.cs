﻿using HarmonyLib;
using MonkeyLoader.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonkeyLoader.Patching
{
    /// <summary>
    /// Contains comparers for <see cref="IMonkey"/>s / derived <see cref="MonkeyBase{TMonkey}"/> instances.
    /// </summary>
    public static class Monkey
    {
        /// <summary>
        /// Gets an <see cref="IMonkey"/>-comparer, that sorts patchers with higher impact first.
        /// </summary>
        public static IComparer<IMonkey> AscendingComparer { get; } = new MonkeyComparer();

        /// <summary>
        /// Gets an <see cref="IMonkey"/>-comparer, that sorts patchers with lower impact first.
        /// </summary>
        public static IComparer<IMonkey> DescendingComparer { get; } = new MonkeyComparer(false);

        /// <summary>
        /// Gets the <see cref="Type"/> of <see cref="IEarlyMonkey"/>.
        /// </summary>
        public static Type EarlyMonkeyType { get; } = typeof(IEarlyMonkey);

        /// <summary>
        /// Gets the <see cref="Type"/> of <see cref="IMonkey"/>.
        /// </summary>
        public static Type MonkeyType { get; } = typeof(IMonkey);

        private sealed class MonkeyComparer : IComparer<IMonkey>
        {
            private readonly int _factor;

            public MonkeyComparer(bool ascending = true)
            {
                _factor = ascending ? 1 : -1;
            }

            /// <inheritdoc/>
            public int Compare(IMonkey x, IMonkey y)
            {
                // If one of the mods has to come before the other,
                // all its patchers have to come before as well
                var modComparison = x.Mod.CompareTo(y.Mod);
                if (modComparison != 0)
                    return _factor * modComparison;

                // Only need the first as they're the highest impact ones.
                var biggestX = x.FeaturePatches.FirstOrDefault();
                var biggestY = y.FeaturePatches.FirstOrDefault();

                // Better declare features if you want to sort high
                if (biggestX is null)
                    return biggestY is null ? TypeNameComparison(x, y) : _factor;

                if (biggestY is null)
                    return -1 * _factor;

                var impactComparison = _factor * biggestX.CompareTo(biggestY);
                if (impactComparison != 0)
                    return _factor * impactComparison;

                // Fall back to type name comparison to avoid false equivalence
                return TypeNameComparison(x, y);
            }

            private int TypeNameComparison(IMonkey x, IMonkey y)
                => _factor * x.GetType().FullName.CompareTo(y.GetType().FullName);
        }
    }

    /// <summary>
    /// Represents the base class for patchers that run after a game's assemblies have been loaded.<br/>
    /// All mod defined derivatives must derive from <see cref="Monkey{TMonkey}"/>,
    /// <see cref="ConfiguredMonkey{TMonkey, TConfigSection}"/>, or another class derived from it.
    /// </summary>
    /// <remarks>
    /// Game assemblies and their types can be directly referenced from these.
    /// </remarks>
    /// <inheritdoc/>
    public abstract class Monkey<TMonkey> : MonkeyBase<TMonkey>
        where TMonkey : Monkey<TMonkey>, new()
    {
        /// <inheritdoc/>
        protected Monkey() : base()
        { }

        /// <inheritdoc/>
        public override sealed bool Run()
        {
            ThrowIfRan();

            Ran = true;
            Logger.Debug(() => "Running OnLoaded!");

            try
            {
                if (!OnLoaded())
                {
                    Failed = true;
                    Logger.Warn(() => "OnLoaded failed!");
                }
            }
            catch (Exception ex)
            {
                Failed = true;
                Logger.Error(ex.LogFormat("OnLoaded threw an Exception:"));
            }

            LogPatches();

            return !Failed;
        }

        /// <summary>
        /// <see cref="Logging.Logger.Debug(Func{object})">Debug</see>-logs
        /// the <see cref="HarmonyLib.Harmony">Harmony</see> patches of this patcher.
        /// </summary>
        protected void LogPatches()
        {
            var patchedMethods = Harmony.GetPatchedMethods();

            if (!patchedMethods.Any())
            {
                Logger.Debug(() => "Did not patch any methods!");
                return;
            }

            Logger.Debug(() => "Patched the following methods:");
            Logger.Debug(patchedMethods.Select(GeneralExtensions.FullDescription));
        }

        /// <summary>
        /// Called right after the game tooling packs and all the game's assemblies have been loaded.<br/>
        /// Use this to apply any patching and return <c>true</c> if it was successful.
        /// </summary>
        /// <remarks>
        /// <i>By default:</i> Applies the <see cref="Harmony"/> patches of the
        /// <see cref="Harmony.PatchCategory(string)">category</see> with this patcher's type's name.<br/>
        /// Easy to apply by using <c>[<see cref="HarmonyPatchCategory"/>(nameof(MyPatcher))]</c> attribute.
        /// </remarks>
        /// <returns><c>true</c> if it ran successfully; otherwise, <c>false</c>.</returns>
        protected virtual bool OnLoaded()
        {
            Harmony.PatchCategory(Type.Assembly, Type.Name);

            return true;
        }
    }
}