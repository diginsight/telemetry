using Diginsight.CAOptions;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public sealed class SmartCacheCoreOptions : ISmartCacheCoreOptions, IDynamicallyPostConfigurable
{
    private Expiration maxAge = Expiration.Never;
    private Expiration absoluteExpiration = Expiration.Never;
    private Expiration slidingExpiration = Expiration.Never;
    private TimeSpan localEntryTolerance = TimeSpan.FromSeconds(10);

    public bool DiscardExternalMiss { get; set; }
    public StorageMode StorageMode { get; set; }

    public Expiration MaxAge
    {
        get => maxAge;
        set => maxAge = value >= Expiration.Zero ? value : Expiration.Zero;
    }

    public DateTimeOffset? MinimumCreationDate { get; private set; }

    public Expiration AbsoluteExpiration
    {
        get => absoluteExpiration;
        set => absoluteExpiration = value >= Expiration.Zero ? value : Expiration.Zero;
    }

    public Expiration SlidingExpiration
    {
        get => slidingExpiration;
        set => slidingExpiration = value >= Expiration.Zero ? value : Expiration.Zero;
    }

    public int LocationPrefetchCount { get; set; } = 5;
    public int LocationMaxParallelism { get; set; } = 2;

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

        public StorageMode StorageMode
        {
            get => filled.StorageMode;
            set => filled.StorageMode = value;
        }

        public Expiration MaxAge
        {
            get => filled.MaxAge;
            set => filled.MaxAge = value;
        }

        public DateTimeOffset? MinimumCreationDate
        {
            get => filled.MinimumCreationDate;
            set => filled.MinimumCreationDate = value;
        }

        public Expiration AbsoluteExpiration
        {
            get => filled.AbsoluteExpiration;
            set => filled.AbsoluteExpiration = value;
        }

        public Expiration SlidingExpiration
        {
            get => filled.SlidingExpiration;
            set => filled.SlidingExpiration = value;
        }

        public int MissValueSizeThreshold
        {
            get => filled.MissValueSizeThreshold;
            set => filled.MissValueSizeThreshold = value;
        }
    }
}
