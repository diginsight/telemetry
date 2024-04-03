namespace Diginsight.SmartCache;

public interface ISmartCacheCoreOptions
{
    bool DiscardExternalMiss { get; }
    bool RedisOnlyCache { get; }

    Expiration MaxAge { get; }

    Expiration AbsoluteExpiration { get; }
    Expiration SlidingExpiration { get; }

    int CompanionPrefetchCount { get; }
    int CompanionMaxParallelism { get; }

    int MissValueSizeThreshold { get; }

    long LowPrioritySizeThreshold { get; }
    long MidPrioritySizeThreshold { get; }

    TimeSpan LocalEntryTolerance { get; }
}
