namespace Diginsight.SmartCache.Externalization.Local;

internal sealed class LocalCacheCompanion : ICacheCompanion
{
    public string SelfLocationId => "<self>";

    public IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    public LocalCacheCompanion(IEnumerable<PassiveCacheLocation> passiveLocations)
    {
        PassiveLocations = passiveLocations;
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
