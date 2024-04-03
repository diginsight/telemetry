using Diginsight.CAOptions;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public sealed class SmartCacheCoreOptions : ISmartCacheCoreOptions, IDynamicallyPostConfigurable
{
    private TimeSpan localEntryTolerance = TimeSpan.FromSeconds(10);
    private TimeSpan maxAge = TimeSpan.MaxValue;
    private TimeSpan absoluteExpiration = TimeSpan.MaxValue;
    private TimeSpan slidingExpiration = TimeSpan.MaxValue;

    public bool DiscardExternalMiss { get; set; }
    public bool RedisOnlyCache { get; set; }

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

    object IDynamicallyPostConfigurable.MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class Filler
    {
        private readonly SmartCacheCoreOptions filled;

        public Filler(SmartCacheCoreOptions filled)
        {
            this.filled = filled;
        }

        public bool DiscardExternalMiss
        {
            get => filled.DiscardExternalMiss;
            set => filled.DiscardExternalMiss = value;
        }

        public bool RedisOnlyCache
        {
            get => filled.RedisOnlyCache;
            set => filled.RedisOnlyCache = value;
        }

        public TimeSpan MaxAge
        {
            get => filled.MaxAge;
            set => filled.MaxAge = value;
        }

        public TimeSpan AbsoluteExpiration
        {
            get => filled.AbsoluteExpiration;
            set => filled.AbsoluteExpiration = value;
        }

        public TimeSpan SlidingExpiration
        {
            get => filled.SlidingExpiration;
            set => filled.SlidingExpiration = value;
        }
    }
}
