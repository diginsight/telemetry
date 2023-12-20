namespace Diginsight.SmartCache;

public sealed class SmartCacheRedisOptions : ISmartCacheRedisOptions
{
    public string? Configuration { get; set; }

    public string KeyPrefix { get; set; } = null!;
}
