using System.ComponentModel;
#if !NET
using System.Collections;
#endif

namespace System.Linq;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{
#if !NET
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

#if !NET10_0_OR_GREATER
    public static IEnumerable<TSource> Reverse<TSource>(this TSource[] array) => ((IEnumerable<TSource>)array).Reverse();
#endif
}
