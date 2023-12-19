namespace Diginsight.SmartCache;

public sealed class SmartCacheRedisOptions : ISmartCacheRedisOptions
{
    public string Configuration { get; set; } = null!;

    public string KeyPrefix { get; set; } = null!;
}
