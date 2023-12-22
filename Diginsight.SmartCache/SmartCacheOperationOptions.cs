namespace Diginsight.SmartCache;

public sealed class SmartCacheOperationOptions
{
    public bool Disabled { get; set; }

    public TimeSpan? MaxAge { get; set; }

    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
}
