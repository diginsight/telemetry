namespace Diginsight.SmartCache;

internal class LocalCacheCompanionProvider : ICacheCompanionProvider
{
    public string SelfLocationId => "<self>";

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public LocalCacheCompanionProvider(RedisCacheLocation? redisLocation = null)
    {
        PassiveLocations = redisLocation is null
            ? Enumerable.Empty<PassiveCacheLocation>()
            : new[] { redisLocation };
    }

    public Task<IEnumerable<CacheCompanion>> GetCompanionsAsync()
    {
        return Task.FromResult(Enumerable.Empty<CacheCompanion>());
    }
}
