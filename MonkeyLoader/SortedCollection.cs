using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyLoader
{
    /// <summary>
    /// Represents a collection which is kept sorted using an <see cref="IComparer{T}"/>
    /// or the <see cref="Comparer{T}.Default">default comparer</see> for <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Duplicate items, <c>null</c> items, and multiple items comparing equal are supported.
    /// </remarks>
    /// <typeparam name="T">The type of the items.</typeparam>
    public class SortedCollection<T> : ICollection<T>
    {
        private readonly IComparer<T> _comparer;
        private readonly List<T> _values;

        /// <inheritdoc/>
        public int Count => _values.Count;

        bool ICollection<T>.IsReadOnly => ((ICollection<T>)_values).IsReadOnly;

        /// <summary>
        /// Gets the element at the specified index in the collection.
        /// </summary>
        /// <param name="index">The index to get the element at.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">When the index is not in the collection.</exception>
        public T this[int index] => _values[index];

        /// <summary>
        /// Constructs an empty sorted collection with the
        /// <see cref="Comparer{T}.Default">default comparer</see> for <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">When <typeparamref name="T"/> is not <see cref="IComparable{T}"/>.</exception>
        public SortedCollection() : this(null, null)
        { }

        /// <summary>
        /// Constructs an empty sorted collection with the given comparer to keep it sorted.
        /// </summary>
        /// <param name="comparer">The comparer to use to keep the collection sorted.</param>
        /// <exception cref="NotSupportedException">When <paramref name="comparer"/> is <c>null</c> and <typeparamref name="T"/> is not <see cref="IComparable{T}"/>.</exception>
        public SortedCollection(IComparer<T>? comparer) : this(null, comparer)
        { }

        /// <summary>
        /// Constructs a sorted collection with the given elements and comparer to keep it sorted.
        /// </summary>
        /// <param name="collection">The elements to add to the collection.</param>
        /// <param name="comparer">The comparer to use to keep the collection sorted.</param>
        /// <exception cref="NotSupportedException">When <paramref name="comparer"/> is <c>null</c> and <typeparamref name="T"/> is not <see cref="IComparable{T}"/>.</exception>
        public SortedCollection(IEnumerable<T>? collection, IComparer<T>? comparer)
        {
            if (comparer is null && !typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
                throw new NotSupportedException("Can't create a sorted collection of an uncomparable type without a comparer!");

            _comparer = comparer ?? Comparer<T>.Default;

            _values = new(collection ?? Array.Empty<T>());
            _values.Sort(_comparer);
        }

        /// <summary>
        /// Constructs a sorted collection with the given elements and
        /// <see cref="Comparer{T}.Default">default comparer</see> for <typeparamref name="T"/>.
        /// </summary>
        /// <param name="collection">The elements to add to the collection.</param>
        /// <exception cref="NotSupportedException">If <typeparamref name="T"/> is not <see cref="IComparable{T}"/>.</exception>
        public SortedCollection(IEnumerable<T>? collection) : this(collection, null)
        { }

        /// <inheritdoc/>
        public void Add(T item) => _values.Insert(FindInsertIndex(item), item);

        /// <summary>
        /// Adds each item in the given sequence to this sorted collection.
        /// </summary>
        /// <param name="collection">The collection of items to add.</param>
        public void AddRange(IEnumerable<T> collection)
            => collection?.Do(Add);

        /// <inheritdoc/>
        public void Clear() => _values.Clear();

        /// <inheritdoc/>
        public bool Contains(T item) => _values.IndexOf(item) != -1;

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

        /// <summary>
        /// Creates a shallow copy of a range of elements this sorted collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns>A shallow copy of a range of elements in this sorted collection.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0. -or- <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> and <paramref name="count"/> do not denote a valid range of elements in this sorted collection.</exception>
        public SortedCollection<T> GetRange(int index, int count)
            => new(_values.GetRange(index, count), _comparer);

        /// <summary>
        /// Attempts to find the first index of the given item.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>The first index of the item if it was found; otherwise, <c>-1</c>.</returns>
        public int IndexOf(T item)
        {
            var i = IndexOfEqualityClass(item);

            if (i < 0)
                return -1;

            // search forwards
            for (; i < _values.Count && _comparer.Compare(item, _values[i]) == 0; ++i)
            {
                if (item?.Equals(_values[i]) ?? _values[i] is null)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Attempts to find the first index in this collection with an item that compares equal to the needle.
        /// </summary>
        /// <param name="needle">The item to search for.</param>
        /// <returns>The last index if equal items are found; otherwise, <c>-1</c>.</returns>
        public int IndexOfEqualityClass(T needle)
        {
            var i = IndexInEqualityClass(needle);

            if (i < 0)
                return -1;

            // search backwards
            while (--i >= 0 && _comparer.Compare(needle, _values[i]) == 0)
            { }

            return ++i;
        }

        /// <summary>
        /// Attempts to find the last index of the given item.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>The last index of the item if it was found; otherwise, <c>-1</c>.</returns>
        public int LastIndexOf(T item)
        {
            var i = LastIndexOfEqualityClass(item);

            if (i < 0)
                return -1;

            // search backwards
            for (; i >= 0 && _comparer.Compare(item, _values[i]) == 0; --i)
            {
                if (item?.Equals(_values[i]) ?? _values[i] is null)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Attempts to find the last index in this collection with an item that compares equal to the needle.
        /// </summary>
        /// <param name="needle">The item to search for.</param>
        /// <returns>The last index if equal items are found; otherwise, <c>-1</c>.</returns>
        public int LastIndexOfEqualityClass(T needle)
        {
            var i = IndexInEqualityClass(needle);

            if (i < 0)
                return -1;

            // search forwards
            while (++i < _values.Count && _comparer.Compare(needle, _values[i]) == 0)
            { }

            return --i;
        }

        /// <inheritdoc/>
        public bool Remove(T item) => _values.Remove(item);

        /// <summary>
        /// Removes the element at the given index.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="IndexOutOfRangeException">When the index is not in the collection.</exception>
        public void RemoveAt(int index) => _values.RemoveAt(index);

        private int FindInsertIndex(T needle)
        {
            var i = IndexInEqualityClass(needle);
            if (i < 0)
                return ~i;

            // search forwards
            while (++i < _values.Count && _comparer.Compare(needle, _values[i]) == 0)
            { }

            return i;
        }

        private int IndexInEqualityClass(T needle) => _values.BinarySearch(needle, _comparer);
    }
}