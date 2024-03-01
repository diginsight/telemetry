using Diginsight.SmartCache.Externalization.Redis;

namespace Diginsight.SmartCache.Externalization.Local;

internal sealed class LocalCacheCompanion : ICacheCompanion
{
    public string SelfLocationId => "<self>";

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public LocalCacheCompanion(RedisCacheLocation? redisLocation = null)
    {
        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    public Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds)
    {
        return Task.FromResult(Enumerable.Empty<ActiveCacheLocation>());
    }

    public Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync()
    {
        return Task.FromResult(Enumerable.Empty<CacheEventNotifier>());
    }
}
