namespace Diginsight.SmartCache;

public sealed class SmartCacheOperationOptions : ISmartCacheOperationOptions
{
    public bool Enabled { get; set; }
    public TimeSpan? MaxAge { get; set; }
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
}
