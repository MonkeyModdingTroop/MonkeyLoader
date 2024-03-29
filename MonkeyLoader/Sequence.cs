using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Wraps an <c><typeparamref name="T"/>[]</c> to use sequence equality semantics.
    /// </summary>
    public readonly struct Sequence<T> : IEquatable<Sequence<T>>, IEquatable<IEnumerable<T>>, IEnumerable<T>
    {
        private readonly T[]? _array;

        /// <summary>
        /// Gets the backing array of this sequence.
        /// </summary>
        public T[] Array => _array ?? System.Array.Empty<T>();

        /// <summary>
        /// Gets the number of elements in this sequence.
        /// </summary>
        public int Length => Array.Length;

        /// <summary>
        /// Gets the element at the specified index in the sequence.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index] => Array[index];

        /// <summary>
        /// Creates a new sequence to wrap the given array.
        /// </summary>
        /// <param name="elements">The array to wrap.</param>
        public Sequence(params T[]? elements)
        {
            _array = elements ?? System.Array.Empty<T>();
        }

        /// <summary>
        /// Creates a new sequence with the elements.
        /// </summary>
        /// <param name="elements">The elements that form the sequence.</param>
        public Sequence(IEnumerable<T>? elements) : this(elements?.ToArray())
        { }

        /// <summary>
        /// Wraps the given array into a sequence.
        /// </summary>
        /// <param name="array">The array to wrap.</param>
        public static implicit operator Sequence<T>(T[]? array) => new(array);

        /// <summary>
        /// Wraps the given element into a sequence.
        /// </summary>
        /// <param name="element">The element to wrap.</param>
        public static implicit operator Sequence<T>(T element) => new(element);

        /// <summary>
        /// Unwraps the given sequence's <see cref="Array">array</see>.
        /// </summary>
        /// <param name="sequence">The sequence to unwrap.</param>
        public static implicit operator T[](Sequence<T> sequence) => sequence.Array;

        /// <summary>
        /// Checks if the left sequence is unequal to the right one.
        /// </summary>
        /// <param name="left">The first sequence.</param>
        /// <param name="right">The second sequence.</param>
        /// <returns><c>true</c> if the sequences are unequal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Sequence<T> left, Sequence<T> right) => !(left == right);

        /// <summary>
        /// Checks if the left sequence is equal to the right one.
        /// </summary>
        /// <param name="left">The first sequence.</param>
        /// <param name="right">The second sequence.</param>
        /// <returns><c>true</c> if the sequences are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Sequence<T> left, Sequence<T> right)
            => ReferenceEquals(left, right) || left.Array.SequenceEqual(right.Array);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Sequence<T> set && Equals(set);

        /// <inheritdoc/>
        public bool Equals(Sequence<T> other) => other == this;

        /// <inheritdoc/>
        public bool Equals(IEnumerable<T> other) => Array.SequenceEqual(other);

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Array.GetEnumerator();

        /// <inheritdoc/>
        public override int GetHashCode()
            => unchecked(Array.Aggregate(0, (acc, element) => (31 * acc) + (element?.GetHashCode() ?? 0)));
    }
}