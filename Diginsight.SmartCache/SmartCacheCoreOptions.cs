using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public sealed class SmartCacheCoreOptions : ISmartCacheCoreOptions
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

    public object MakeFiller() => new Filler(this);

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class Filler
    {
        private readonly SmartCacheCoreOptions owner;

        public Filler(SmartCacheCoreOptions owner)
        {
            this.owner = owner;
        }

        public bool DiscardExternalMiss
        {
            get => owner.DiscardExternalMiss;
            set => owner.DiscardExternalMiss = value;
        }

        public bool RedisOnlyCache
        {
            get => owner.RedisOnlyCache;
            set => owner.RedisOnlyCache = value;
        }

        public TimeSpan MaxAge
        {
            get => owner.MaxAge;
            set => owner.MaxAge = value;
        }

        public TimeSpan AbsoluteExpiration
        {
            get => owner.AbsoluteExpiration;
            set => owner.AbsoluteExpiration = value;
        }

        public TimeSpan SlidingExpiration
        {
            get => owner.SlidingExpiration;
            set => owner.SlidingExpiration = value;
        }
    }
}
