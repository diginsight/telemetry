namespace Diginsight.SmartCache.Externalization.Redis;

public sealed class SmartCacheRedisOptions : ISmartCacheRedisOptions
{
    public string? Configuration { get; set; }

    public string KeyPrefix { get; set; } = null!;
}
