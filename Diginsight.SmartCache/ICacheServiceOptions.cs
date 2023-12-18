namespace Diginsight.SmartCache;

public interface ICacheServiceOptions
{
    int DefaultMaxAge { get; }

    int AbsoluteExpiration { get; }
    int SlidingExpiration { get; }

    TimeSpan CompanionRequestTimeout { get; }
    int CompanionPrefetchCount { get; }
    int CompanionMaxParallelism { get; }

    long SizeLimit { get; }
    int MissValueSizeThreshold { get; }

    long LowPrioritySizeThreshold { get; }
    long MidPrioritySizeThreshold { get; }

    long ReadLatencyThreshold { get; }

    string RedisKeyPrefix { get; }

    TimeSpan LocalEntryTolerance { get; }
}
