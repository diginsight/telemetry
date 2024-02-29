namespace Diginsight.SmartCache.Externalization;

public interface ICacheCompanionProvider
{
    string SelfLocationId { get; }

    IEnumerable<PassiveCacheLocation> PassiveLocations { get; }

    Task<IEnumerable<CacheCompanion>> GetCompanionsAsync();
}
