﻿using HarmonyLib;
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// A selector following the try-pattern.
    /// </summary>
    /// <typeparam name="TSource">The type of the source item.</typeparam>
    /// <typeparam name="TResult">The type of the result item.</typeparam>
    /// <param name="source">The source item to transform.</param>
    /// <param name="result">The result item when successful, or <c>null</c> otherwise.</param>
    /// <returns>Whether the transformation was successful.</returns>
    public delegate bool TrySelector<TSource, TResult>(TSource source, [NotNullWhen(true)] out TResult? result);

    /// <summary>
    /// Contains handy extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Determines whether all elements of a boolean sequence are <c>true</c>.<br/>
        /// </summary>
        /// <param name="source">The source sequence to check.</param>
        /// <returns>
        /// <c>true</c> if every element of the source sequence is <c>true</c>,
        /// or if the sequence is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool All(this IEnumerable<bool> source)
        {
            foreach (var item in source)
            {
                if (!item)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether any element of a boolean sequence is <c>true</c>.<br/>
        /// </summary>
        /// <param name="source">The source sequence to check.</param>
        /// <returns><c>true</c> if any element of the source sequence is <c>true</c>; otherwise, <c>false</c>.</returns>
        public static bool AnyFalse(this IEnumerable<bool> source)
        {
            foreach (var item in source)
            {
                if (!item)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether any element of a boolean sequence is <c>true</c>.<br/>
        /// </summary>
        /// <param name="source">The source sequence to check.</param>
        /// <returns><c>true</c> if any element of the source sequence is <c>true</c>; otherwise, <c>false</c>.</returns>
        public static bool AnyTrue(this IEnumerable<bool> source)
        {
            foreach (var item in source)
            {
                if (item)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Wraps the given <see cref="IEnumerable{T}"/> in an iterator to prevent casting it to its underlaying type.
        /// </summary>
        /// <typeparam name="T">The type of items in the sequence.</typeparam>
        /// <param name="enumerable">The enumerable to wrap.</param>
        /// <returns>An iterator wrapping the input enumerable.</returns>
        public static IEnumerable<T> AsSafeEnumerable<T>(this IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                yield return item;
        }

        /// <summary>
        /// Appends a single item to a sequence.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="source">The input sequence.</param>
        /// <param name="item">The item to append.</param>
        /// <returns>The sequence with an appended item.</returns>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
        {
            foreach (var sourceItem in source)
                yield return sourceItem;

            yield return item;
        }

        /// <summary>
        /// Outputs all nodes that are part of cycles, given an expression that produces a set of connected nodes.
        /// </summary>
        /// <param name="nodes">The input nodes.</param>
        /// <param name="identifier">Gets the dependency identifier of a node.</param>
        /// <param name="connected">The expression producing connected nodes.</param>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TDependency">The dependency connection type.</typeparam>
        /// <returns>All input nodes that are part of a cycle.</returns>
        public static IEnumerable<TNode> FindCycles<TNode, TDependency>(this IEnumerable<TNode> nodes, Func<TNode, TDependency> identifier, Func<TNode, IEnumerable<TDependency>> connected)
            where TNode : notnull
            where TDependency : notnull
        {
            var currentPath = new Stack<TNode>();
            var visited = new HashSet<TNode>();
            var dependencies = nodes.ToDictionary(node => node, node => new HashSet<TDependency>(connected(node)));

            while (dependencies.Count > 0)
            {
                var key = dependencies.FirstOrDefault(x => x.Value.Count == 0).Key
                    ?? throw new ArgumentException($"Cyclic dependencies are not allowed!{Environment.NewLine}" +
                    $"Sorted: {string.Join(", ", nodes.Select(identifier).Except(dependencies.Keys.Select(identifier)))}{Environment.NewLine}" +
                    $"Unsorted:{Environment.NewLine}" +
                    $"    {string.Join($"{Environment.NewLine}    ", dependencies.Select(element => $"{identifier(element.Key)}:{Environment.NewLine}        {string.Join($"{Environment.NewLine}        ", element.Value)}"))}");

                dependencies.Remove(key);

                var id = identifier(key);
                foreach (var updateElement in dependencies)
                    updateElement.Value.Remove(id);

                yield return key;
            }
        }

        /// <summary>
        /// Formats an <see cref="Exception"/> with a message.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <param name="message">The message to prepend.</param>
        /// <returns>The formatted message and exception.</returns>
        public static string Format(this Exception ex, string message)
            => $"{message}{Environment.NewLine}{ex.Format()}";

        /// <summary>
        /// Formats an <see cref="Exception"/>.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <returns>The formatted exception.</returns>
        public static string Format(this Exception ex)
        {
            return ex switch
            {
                AggregateException aggrex => $"{ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{string.Join(Environment.NewLine, aggrex.InnerExceptions.Select(Format))}{Environment.NewLine}--------------------",
                _ => $"{ex.Message}{Environment.NewLine}{ex.StackTrace}{(ex.InnerException is Exception innerEx ? $"{Environment.NewLine}{innerEx.Format()}" : "")}{Environment.NewLine}--------------------"
            };
        }

        public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;

            value = valueFactory();
            dictionary.Add(key, value);

            return value;
        }

        public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;

            value = new TValue();
            dictionary.Add(key, value);

            return value;
        }

        /// <summary>
        /// Filters a source sequence of <see cref="Type"/>s to only contain the ones instantiable
        /// without parameters and assignable to <typeparamref name="TInstance"/>.
        /// </summary>
        /// <typeparam name="TInstance">The type that sequence items must be assignable to.</typeparam>
        /// <param name="types">The source sequence of types to filter.</param>
        /// <returns>A sequence of instantiable types assignable to <typeparamref name="TInstance"/>.</returns>
        public static IEnumerable<Type> Instantiable<TInstance>(this IEnumerable<Type> types)
        {
            var instanceType = typeof(TInstance);

            foreach (var type in types)
            {
                if (!type.IsAbstract && !type.ContainsGenericParameters && instanceType.IsAssignableFrom(type)
                    && (type.IsValueType || type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) is not null))
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Tries to cast every item from the <paramref name="source"/> to <typeparamref name="TTo"/>.
        /// </summary>
        /// <typeparam name="TFrom">The items in the source sequence.</typeparam>
        /// <typeparam name="TTo">The items in the result sequence.</typeparam>
        /// <param name="source">The items to try and cast.</param>
        /// <returns>All items from the source that were castable to <typeparamref name="TTo"/> and not <c>null</c>.</returns>
        public static IEnumerable<TTo> SelectCastable<TFrom, TTo>(this IEnumerable<TFrom?> source)
        {
            foreach (var item in source)
            {
                if (item is TTo toItem)
                    yield return toItem;
            }
        }

        /// <summary>
        /// Performs a topological sort of the input <paramref name="nodes"/>, given an expression that produces a set of connected nodes.
        /// </summary>
        /// <param name="nodes">The input nodes.</param>
        /// <param name="identifier">Gets the dependency identifier of a node.</param>
        /// <param name="connected">The expression producing connected nodes.</param>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <typeparam name="TDependency">The dependency connection type.</typeparam>
        /// <returns>The input nodes, sorted .</returns>
        /// <exception cref="ArgumentException">Thrown if a cyclic dependency is found.</exception>
        public static IEnumerable<TNode> TopologicalSort<TNode, TDependency>(this IEnumerable<TNode> nodes, Func<TNode, TDependency> identifier, Func<TNode, IEnumerable<TDependency>> connected)
            where TNode : notnull
            where TDependency : notnull
        {
            var elements = nodes.ToDictionary(node => node, node => new HashSet<TDependency>(connected(node)));

            while (elements.Count > 0)
            {
                var key = elements.FirstOrDefault(x => x.Value.Count == 0).Key
                    ?? throw new ArgumentException($"Cyclic dependencies are not allowed!{Environment.NewLine}" +
                    $"Sorted: {string.Join(", ", nodes.Select(identifier).Except(elements.Keys.Select(identifier)))}{Environment.NewLine}" +
                    $"Unsorted:{Environment.NewLine}" +
                    $"    {string.Join($"{Environment.NewLine}    ", elements.Select(element => $"{identifier(element.Key)}:{Environment.NewLine}        {string.Join($"{Environment.NewLine}        ", element.Value)}"))}");

                elements.Remove(key);

                var id = identifier(key);
                foreach (var updateElement in elements)
                    updateElement.Value.Remove(id);

                yield return key;
            }
        }

        /// <summary>
        /// Performs a topological sort of the input <paramref name="nodes"/>, given an expression that produces a set of connected nodes.
        /// </summary>
        /// <param name="nodes">The input nodes.</param>
        /// <param name="connected">The expression producing connected nodes.</param>
        /// <typeparam name="TNode">The node type.</typeparam>
        /// <returns>The input nodes, sorted.</returns>
        /// <exception cref="ArgumentException">Thrown if a cyclic dependency is found.</exception>
        public static IEnumerable<TNode> TopologicalSort<TNode>(this IEnumerable<TNode> nodes, Func<TNode, IEnumerable<TNode>> connected)
            where TNode : notnull => nodes.TopologicalSort(node => node, connected);

        /// <summary>
        /// Individually calls every method from a <see cref="Delegate"/>'s invocation list in a try-catch-block,
        /// collecting any <see cref="Exception"/>s into an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="del">The delegate to safely invoke.</param>
        /// <param name="args">The arguments for the invocation.</param>
        /// <exception cref="AggregateException">Thrown when any invoked methods threw. Contains all nested Exceptions.</exception>
        public static void TryInvokeAll(this Delegate del, params object[] args)
            => del.GetInvocationList().TryInvokeAll(args);

        /// <summary>
        /// Individually calls all <paramref name="delegates"/> in a try-catch-block,
        /// collecting any <see cref="Exception"/>s into an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="delegates">The delegates to safely invoke.</param>
        /// <param name="args">The arguments for the invocation.</param>
        /// <exception cref="AggregateException">Thrown when any invoked methods threw. Contains all nested Exceptions.</exception>
        public static void TryInvokeAll(this IEnumerable<Delegate> delegates, params object[] args)
        {
            var exceptions = new List<Exception>();

            foreach (var handler in delegates)
            {
                try
                {
                    handler.Method.Invoke(handler.Target, args);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Tries to transform each item in the <paramref name="source"/> sequence using the <paramref name="trySelector"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of items in the source sequence.</typeparam>
        /// <typeparam name="TResult">The type of items in the result sequence.</typeparam>
        /// <param name="source">The source sequence to transform.</param>
        /// <param name="trySelector">A selector following the try-pattern.</param>
        /// <returns>A result sequence containing only the successfully transformed items.</returns>
        public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, TrySelector<TSource, TResult> trySelector)
        {
            foreach (var item in source)
            {
                if (trySelector(item, out var result))
                    yield return result;
            }
        }

        /// <summary>
        /// Turns a single item into a sequence.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="item">The item to make into a sequence.</param>
        /// <returns>The item as a sequence.</returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}