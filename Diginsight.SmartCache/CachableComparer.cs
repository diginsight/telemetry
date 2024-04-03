using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

public sealed class CachableComparer : IEqualityComparer<ICachable>
{
    private readonly ICacheKeyService cacheKeyService;
    private readonly IEqualityComparer<object> keyComparer;

    public CachableComparer(ICacheKeyService cacheKeyService, IEqualityComparer<object>? keyComparer = null)
    {
        this.cacheKeyService = cacheKeyService;
        this.keyComparer = keyComparer ?? EqualityComparer<object>.Default;
    }

    public bool Equals(ICachable? x, ICachable? y)
    {
        return ReferenceEquals(x, y) ||
            (x?.GetType() == y?.GetType() &&
                keyComparer.Equals(x!.ToKey(cacheKeyService), y!.ToKey(cacheKeyService)));
    }

    public int GetHashCode(ICachable obj)
    {
        return obj.ToKey(cacheKeyService) is { Success: true } result
            ? keyComparer.GetHashCode(result.UntypedKey!)
            : RuntimeHelpers.GetHashCode(obj);
    }
}
