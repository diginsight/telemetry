namespace Diginsight.SmartCache.Externalization.Redis;

public interface ISmartCacheRedisOptions
{
    string? Configuration { get; }
    string KeyPrefix { get; }
}
