namespace Diginsight.SmartCache;

public sealed class SmartCacheOperationOptions
{
    public bool Disabled { get; set; }

    public Expiration? MaxAge { get; set; }

    public Expiration? AbsoluteExpiration { get; set; }
    public Expiration? SlidingExpiration { get; set; }

    public SmartCacheOperationOptions Clone() => (SmartCacheOperationOptions)MemberwiseClone();
}
