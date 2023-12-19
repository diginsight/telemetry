namespace Diginsight.SmartCache;

public sealed class SmartCacheServiceOptions : ISmartCacheServiceOptions
{
    private TimeSpan localEntryTolerance = TimeSpan.FromSeconds(10);

    public TimeSpan DefaultMaxAge { get; set; }

    public TimeSpan AbsoluteExpiration { get; set; }
    public TimeSpan SlidingExpiration { get; set; }

    public int CompanionPrefetchCount { get; set; } = 5;
    public int CompanionMaxParallelism { get; set; } = 2;

    public long SizeLimit { get; set; } = 10_000_000;
    public int MissValueSizeThreshold { get; set; } = 5_000;

    public long LowPrioritySizeThreshold { get; set; } = 20_000;
    public long MidPrioritySizeThreshold { get; set; } = 10_000;

    public long ReadLatencyThreshold { get; set; } = 150;

    public TimeSpan LocalEntryTolerance
    {
        get => localEntryTolerance;
        set => localEntryTolerance = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }
}
