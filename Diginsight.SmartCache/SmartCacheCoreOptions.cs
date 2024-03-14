namespace Diginsight.SmartCache;

public sealed class SmartCacheCoreOptions : ISmartCacheCoreOptions
{
    private TimeSpan localEntryTolerance = TimeSpan.FromSeconds(10);
    private TimeSpan maxAge = TimeSpan.MaxValue;
    private TimeSpan absoluteExpiration = TimeSpan.MaxValue;
    private TimeSpan slidingExpiration = TimeSpan.MaxValue;

    public TimeSpan MaxAge
    {
        get => maxAge;
        set => maxAge = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    public TimeSpan AbsoluteExpiration
    {
        get => absoluteExpiration;
        set => absoluteExpiration = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    public TimeSpan SlidingExpiration
    {
        get => slidingExpiration;
        set => slidingExpiration = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    public int CompanionPrefetchCount { get; set; } = 5;
    public int CompanionMaxParallelism { get; set; } = 2;

    public int MissValueSizeThreshold { get; set; } = 5_000;

    public long LowPrioritySizeThreshold { get; set; } = 20_000;
    public long MidPrioritySizeThreshold { get; set; } = 10_000;

    public TimeSpan LocalEntryTolerance
    {
        get => localEntryTolerance;
        set => localEntryTolerance = value >= TimeSpan.Zero ? value : TimeSpan.Zero;
    }
}
