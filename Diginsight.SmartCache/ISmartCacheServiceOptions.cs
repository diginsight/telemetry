namespace Diginsight.SmartCache;

public interface ISmartCacheServiceOptions
{
    TimeSpan DefaultMaxAge { get; }

    TimeSpan AbsoluteExpiration { get; }
    TimeSpan SlidingExpiration { get; }

    TimeSpan CompanionRequestTimeout { get; }
    int CompanionPrefetchCount { get; }
    int CompanionMaxParallelism { get; }

    long SizeLimit { get; }
    int MissValueSizeThreshold { get; }

    long LowPrioritySizeThreshold { get; }
    long MidPrioritySizeThreshold { get; }

    long ReadLatencyThreshold { get; }

    TimeSpan LocalEntryTolerance { get; }
}
