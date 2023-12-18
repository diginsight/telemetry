using System.Runtime.CompilerServices;

namespace Diginsight.SmartCache;

public sealed class CacheEntityComparer : IEqualityComparer<ICachable>
{
    private readonly ICacheKeyService cacheKeyService;
    private readonly IEqualityComparer<object> keyComparer;

    public CacheEntityComparer(ICacheKeyService cacheKeyService, IEqualityComparer<object>? keyComparer = null)
    {
        this.cacheKeyService = cacheKeyService;
        this.keyComparer = keyComparer ?? EqualityComparer<object>.Default;
    }

    public bool Equals(ICachable? x, ICachable? y)
    {
        return ReferenceEquals(x, y) ||
            (x?.GetType() == y?.GetType() &&
                keyComparer.Equals(x!.GetKey(cacheKeyService), y!.GetKey(cacheKeyService)));
    }

    public int GetHashCode(ICachable obj)
    {
        return obj.GetKey(cacheKeyService) is { } key
            ? keyComparer.GetHashCode(key)
            : RuntimeHelpers.GetHashCode(obj);
    }
}
