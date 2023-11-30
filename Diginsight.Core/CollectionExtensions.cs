#if !NET6_0_OR_GREATER
using System.Collections;
#endif

namespace Diginsight;

public static class CollectionExtensions
{
    public static int FirstIndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
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

    public static int LastIndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
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

    public static IEnumerable<int> IndexesWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
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

    public static bool SequenceEquivalent<T>(this IEnumerable<T> first, IEnumerable<T> second)
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

    public static bool SequenceEquivalent<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T>? comparer)
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

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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
#endif

    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> collection)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (target is List<T> list)
        {
            list.AddRange(collection);
        }
        else
        {
            foreach (T item in collection)
            {
                target.Add(item);
            }
        }
    }

#if !NET6_0_OR_GREATER
    public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource>? source, out int count)
    {
        switch (source)
        {
            case null:
                throw new ArgumentNullException(nameof(source));

            case ICollection<TSource> coll:
                count = coll.Count;
                return true;

            case ICollection coll:
                count = coll.Count;
                return true;

            default:
                count = 0;
                return false;
        }
    }
#endif
}
