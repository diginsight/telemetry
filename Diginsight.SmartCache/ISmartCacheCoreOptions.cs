namespace Diginsight.SmartCache;

public interface ISmartCacheCoreOptions
{
    bool DiscardExternalMiss { get; }
    bool PassiveOnlyCache { get; }

    Expiration MaxAge { get; }
    DateTimeOffset? MinimumCreationDate { get; }

    Expiration AbsoluteExpiration { get; }
    Expiration SlidingExpiration { get; }

    int LocationPrefetchCount { get; }
    int LocationMaxParallelism { get; }

    int MissValueSizeThreshold { get; }

    long LowPrioritySizeThreshold { get; }
    long MidPrioritySizeThreshold { get; }

    TimeSpan LocalEntryTolerance { get; }
}
