namespace Diginsight.SmartCache;

public interface ISmartCacheCoreOptions
{
    bool DiscardExternalMiss { get; }
    bool RedisOnlyCache { get; }

    TimeSpan MaxAge { get; }

    TimeSpan AbsoluteExpiration { get; }
    TimeSpan SlidingExpiration { get; }

    int CompanionPrefetchCount { get; }
    int CompanionMaxParallelism { get; }

    int MissValueSizeThreshold { get; }

    long LowPrioritySizeThreshold { get; }
    long MidPrioritySizeThreshold { get; }

    TimeSpan LocalEntryTolerance { get; }
}
