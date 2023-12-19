namespace Diginsight.SmartCache;

public interface ISmartCacheRedisOptions
{
    string Configuration { get; }
    string KeyPrefix { get; }
}
