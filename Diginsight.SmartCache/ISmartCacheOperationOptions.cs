namespace Diginsight.SmartCache;

public interface ISmartCacheOperationOptions
{
    bool Enabled { get; }
    TimeSpan? MaxAge { get; }
    TimeSpan? AbsoluteExpiration { get; }
    TimeSpan? SlidingExpiration { get; }
}
