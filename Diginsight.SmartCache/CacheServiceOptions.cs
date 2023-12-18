namespace Diginsight.SmartCache;

public sealed class CacheServiceOptions : ICacheServiceOptions
{
    private TimeSpan localEntryTolerance = TimeSpan.FromSeconds(10);

    public int DefaultMaxAge { get; set; }

    public int AbsoluteExpiration { get; set; }
    public int SlidingExpiration { get; set; }

    public TimeSpan CompanionRequestTimeout { get; set; }
    public int CompanionPrefetchCount { get; set; } = 5;
    public int CompanionMaxParallelism { get; set; } = 2;

    public long SizeLimit { get; set; } = 10_000_000;
    public int MissValueSizeThreshold { get; set; } = 5_000;

    public long LowPrioritySizeThreshold { get; set; } = 20_000;
    public long MidPrioritySizeThreshold { get; set; } = 10_000;

    public long ReadLatencyThreshold { get; set; } = 150;

    public string RedisKeyPrefix { get; set; } = null!;

    public TimeSpan LocalEntryTolerance
    {
        get => localEntryTolerance;
        set => localEntryTolerance = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }
}
