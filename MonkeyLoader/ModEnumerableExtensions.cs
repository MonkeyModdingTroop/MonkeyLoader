﻿using MonkeyLoader.Meta;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Contains helpers to get the <see cref="IMonkey"/>s and <see cref="IEarlyMonkey"/>s of <see cref="Mod"/>s.
    /// </summary>
    public static class ModEnumerableExtensions
    {
        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all given <paramref name="mods"/> in no particular order.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <returns>All <see cref="IEarlyMonkey"/>s of the given <paramref name="mods"/>.</returns>
        public static IEarlyMonkey[] GetEarlyMonkeys(this IEnumerable<Mod> mods)
            => mods.SelectMany(mod => mod.EarlyMonkeys).ToArray();

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all given <paramref name="mods"/> in topological order.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <returns>All <see cref="IEarlyMonkey"/>s of the given <paramref name="mods"/> in topological order.</returns>
        public static IEarlyMonkey[] GetEarlyMonkeysAscending(this IEnumerable<Mod> mods)
            => mods.GetSortedEarlyMonkeys(Monkey.AscendingComparer);

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in topological order.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <returns>The <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in topological order.</returns>
        public static IEarlyMonkey[] GetEarlyMonkeysAscending(this Mod mod)
            => mod.GetSortedEarlyMonkeys(Monkey.AscendingComparer);

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in reverse-topological order.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <returns>The <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in reverse-topological order.</returns>
        public static IEarlyMonkey[] GetEarlyMonkeysDescending(this Mod mod)
            => mod.GetSortedEarlyMonkeys(Monkey.DescendingComparer);

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all given <paramref name="mods"/> in reverse-topological order.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <returns>All <see cref="IEarlyMonkey"/>s of the given <paramref name="mods"/> in reverse-topological order.</returns>
        public static IEarlyMonkey[] GetEarlyMonkeysDescending(this IEnumerable<Mod> mods)
            => mods.GetSortedEarlyMonkeys(Monkey.DescendingComparer);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all given <paramref name="mods"/> in no particular order.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IMonkey"/>s of.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mods"/>.</returns>
        public static IMonkey[] GetMonkeys(this IEnumerable<Mod> mods)
            => mods.SelectMany(mod => mod.Monkeys).ToArray();

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all given <paramref name="mods"/> in topological order.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IMonkey"/>s of.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mods"/> in topological order.</returns>
        public static IMonkey[] GetMonkeysAscending(this IEnumerable<Mod> mods)
            => mods.GetSortedMonkeys(Monkey.AscendingComparer);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of the given <paramref name="mod"/> in topological order.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IMonkey"/>s of.</param>
        /// <returns>The <see cref="IMonkey"/>s of the given <paramref name="mod"/> in topological order.</returns>
        public static IMonkey[] GetMonkeysAscending(this Mod mod)
            => mod.GetSortedMonkeys(Monkey.AscendingComparer);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all given <paramref name="mod"/> in reverse-topological order.
        /// </summary>
        /// <param name="mod">The mods to get the <see cref="IMonkey"/>s of.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mod"/> in reverse-topological order.</returns>
        public static IMonkey[] GetMonkeysDescending(this IEnumerable<Mod> mod)
            => mod.GetSortedMonkeys(Monkey.DescendingComparer);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of the given <paramref name="mod"/> in reverse-topological order.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IMonkey"/>s of.</param>
        /// <returns>The <see cref="IMonkey"/>s of the given <paramref name="mod"/> in reverse-topological order.</returns>
        public static IMonkey[] GetMonkeysDescending(this Mod mod)
            => mod.GetSortedMonkeys(Monkey.DescendingComparer);

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in the order defined by the given <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> defining the order.</param>
        /// <returns>The <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in the defined order.</returns>
        public static IEarlyMonkey[] GetSortedEarlyMonkeys(this Mod mod, Comparison<IEarlyMonkey> comparison)
        {
            var earlyMonkeys = mod.EarlyMonkeys.ToArray();
            Array.Sort(earlyMonkeys, comparison);

            return earlyMonkeys;
        }

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all given <paramref name="mods"/> in the order defined by the given <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> defining the order.</param>
        /// <returns>All <see cref="IEarlyMonkey"/>s of the given <paramref name="mods"/> in the defined order.</returns>
        public static IEarlyMonkey[] GetSortedEarlyMonkeys(this IEnumerable<Mod> mods, Comparison<IEarlyMonkey> comparison)
        {
            var earlyMonkeys = mods.GetEarlyMonkeys();
            Array.Sort(earlyMonkeys, comparison);

            return earlyMonkeys;
        }

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of all given <paramref name="mods"/> in the order defined by the given <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> defining the order.</param>
        /// <returns>All <see cref="IEarlyMonkey"/>s of the given <paramref name="mods"/> in the defined order.</returns>
        public static IEarlyMonkey[] GetSortedEarlyMonkeys(this IEnumerable<Mod> mods, IComparer<IEarlyMonkey> comparer)
            => mods.GetSortedEarlyMonkeys(comparer.Compare);

        /// <summary>
        /// Gets the <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in the order defined by the given <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IEarlyMonkey"/>s of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> defining the order.</param>
        /// <returns>The <see cref="IEarlyMonkey"/>s of the given <paramref name="mod"/> in the defined order.</returns>
        public static IEarlyMonkey[] GetSortedEarlyMonkeys(this Mod mod, IComparer<IEarlyMonkey> comparer)
            => mod.GetSortedEarlyMonkeys(comparer.Compare);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of the given <paramref name="mod"/> in the order defined by the given <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IMonkey"/>s of.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> defining the order.</param>
        /// <returns>The <see cref="IMonkey"/>s of the given <paramref name="mod"/> in the defined order.</returns>
        public static IMonkey[] GetSortedMonkeys(this Mod mod, Comparison<IMonkey> comparison)
        {
            var monkeys = mod.Monkeys.ToArray();
            Array.Sort(monkeys, comparison);

            return monkeys;
        }

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all given <paramref name="mods"/> in the order defined by the given <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IMonkey"/>s of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> defining the order.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mods"/> in the defined order.</returns>
        public static IMonkey[] GetSortedMonkeys(this IEnumerable<Mod> mods, IComparer<IMonkey> comparer)
            => mods.GetSortedMonkeys(comparer.Compare);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of the given <paramref name="mod"/> in the order defined by the given <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="mod">The mod to get the <see cref="IMonkey"/>s of.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> defining the order.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mod"/> in the defined order.</returns>
        public static IMonkey[] GetSortedMonkeys(this Mod mod, IComparer<IMonkey> comparer)
            => mod.GetSortedMonkeys(comparer.Compare);

        /// <summary>
        /// Gets the <see cref="IMonkey"/>s of all given <paramref name="mods"/> in the order defined by the given <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="mods">The mods to get the <see cref="IMonkey"/>s of.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> defining the order.</param>
        /// <returns>All <see cref="IMonkey"/>s of the given <paramref name="mods"/> in the defined order.</returns>
        public static IMonkey[] GetSortedMonkeys(this IEnumerable<Mod> mods, Comparison<IMonkey> comparison)
        {
            var monkeys = mods.GetMonkeys();
            Array.Sort(monkeys, comparison);

            return monkeys;
        }
    }
}