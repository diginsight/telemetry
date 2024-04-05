using System.ComponentModel;

#if !NET6_0_OR_GREATER
using System.Collections;
#endif

namespace System.Linq;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{
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
