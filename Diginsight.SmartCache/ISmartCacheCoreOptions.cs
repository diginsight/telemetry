namespace Diginsight.SmartCache;

public interface ISmartCacheCoreOptions
{
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
