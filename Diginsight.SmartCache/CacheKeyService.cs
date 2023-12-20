using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public sealed class CacheKeyService : ICacheKeyService
{
    public static readonly ICacheKeyService Empty = new CacheKeyService(Enumerable.Empty<ICacheKeyProvider>());

    private readonly IEnumerable<ICacheKeyProvider> cacheKeyProviders;

    public CacheKeyService(IEnumerable<ICacheKeyProvider> cacheKeyProviders)
    {
        this.cacheKeyProviders = cacheKeyProviders;
    }

    public bool TryToKey(object? obj, [NotNullWhen(true)] out ICacheKey? key)
    {
        switch (obj)
        {
            case null:
                key = null;
                return false;

            case ICacheKey k:
                key = k;
                return true;

            case ICachable c:
                key = c.GetKey(this);
                return true;
        }

        foreach (ICacheKeyProvider provider in cacheKeyProviders)
        {
            if (provider.TryToKey(this, obj, out ICacheKey? key1))
            {
                key = key1;
                return true;
            }
        }

        key = null;
        return false;
    }
}
