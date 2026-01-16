using System.ComponentModel;

namespace Diginsight;

/// <summary>
/// Provides extension methods for collections.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CollectionExtensions
{
    /// <typeparam name="T">The type of the elements of the source.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to search.</param>
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Returns the index of the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The zero-based index of the first element that matches the predicate, or -1 if no such element is found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> or <paramref name="predicate" /> is <c>null</c>.</exception>
        public int FirstIndexWhere(Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            foreach (T item in source)
            {
                if (predicate(item))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The zero-based index of the last element that matches the predicate, or -1 if no such element is found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> or <paramref name="predicate" /> is <c>null</c>.</exception>
        public int LastIndexWhere(Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            int index = 0;
            ICollection<T> reverse = source.Reverse().ToArray();
            int count = reverse.Count;
            foreach (T item in reverse)
            {
                if (predicate(item))
                {
                    return count - 1 - index;
                }
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Returns the indexes of all elements in a sequence that satisfy a specified condition.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="IEnumerable{Int32}" /> that contains the indexes of the elements that match the predicate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> or <paramref name="predicate" /> is <c>null</c>.</exception>
        public IEnumerable<int> IndexesWhere(Func<T, bool> predicate)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            return source
                .Select(static (item, index) => (item, index))
                .Where(x => predicate(x.item))
                .Select(static x => x.index);
        }
    }

    /// <param name="first">The first sequence to compare.</param>
    /// <typeparam name="T">The type of the elements of the sequences.</typeparam>
    extension<T>(IEnumerable<T> first)
    {
        /// <summary>
        /// Determines whether two sequences are equivalent, i.e., contain the same elements in the same quantity, regardless of order.
        /// </summary>
        /// <param name="second">The second sequence to compare.</param>
        /// <returns><c>true</c> if the sequences are equivalent; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="first" /> or <paramref name="second" /> is <c>null</c>.</exception>
        public bool SequenceEquivalent(IEnumerable<T> second)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            IList<T> list = second.ToList();
            foreach (T item in first)
            {
                if (list.Count == 0)
                    return false;
                if (!list.Remove(item))
                    return false;
            }
            return list.Count == 0;
        }

        /// <summary>
        /// Determines whether two sequences are equivalent, i.e., contain the same elements in the same quantity, regardless of order, using a specified comparer.
        /// </summary>
        /// <param name="second">The second sequence to compare.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare elements.</param>
        /// <returns><c>true</c> if the sequences are equivalent; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="first" /> or <paramref name="second" /> is <c>null</c>.</exception>
        public bool SequenceEquivalent(IEnumerable<T> second, IEqualityComparer<T>? comparer)
        {
            if (comparer is null)
            {
                return first.SequenceEquivalent(second);
            }

            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            List<T> list = second.ToList();
            foreach (T x1 in first)
            {
                if (list.Count == 0)
                    return false;
                int index = list.FindIndex(x2 => comparer.Equals(x1, x2));
                if (index < 0)
                    return false;
                list.RemoveAt(index);
            }
            return list.Count == 0;
        }
    }

    /// <summary>
    /// Returns a hash code for a sequence of elements, using a specified comparer.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="comparer">An <see cref="IEqualityComparer{T}" /> to compare elements.</param>
    /// <returns>A hash code for the sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source" /> is <c>null</c>.</exception>
    public static int SequenceGetHashCode<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        HashCode hashCode = new ();
        foreach (T item in source)
        {
            hashCode.Add(item, comparer);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="ICollection{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the target collection.</typeparam>
    /// <param name="target">The target collection to add elements to.</param>
    /// <param name="collection">The collection whose elements should be added to the end of the target collection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target" /> or <paramref name="collection" /> is <c>null</c>.</exception>
    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> collection)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        IEnumerable<T> itemsToAdd = collection.Except(target);

        if (target is List<T> list)
        {
            list.AddRange(itemsToAdd);
        }
        else
        {
            foreach (T item in itemsToAdd)
            {
                target.Add(item);
            }
        }
    }
}
