namespace Diginsight.SmartCache;

public sealed class SmartCacheMiddlewareOptions : ISmartCacheMiddlewareOptions
{
    public string? RootPath { get; set; }

    public string? GetPathSegment { get; set; }

    public string? CacheMissPathSegment { get; set; }

    public string? InvalidatePathSegment { get; set; }
}
