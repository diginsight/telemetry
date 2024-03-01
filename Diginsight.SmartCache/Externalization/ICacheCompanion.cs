namespace Diginsight.SmartCache.Externalization;

public interface ICacheCompanion
{
    string SelfLocationId { get; }

    IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    Task<IEnumerable<ActiveCacheLocation>> GetActiveLocationsAsync(IEnumerable<string> locationIds);

    Task<IEnumerable<CacheEventNotifier>> GetAllEventNotifiersAsync();
}
