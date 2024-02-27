namespace Diginsight.SmartCache;

public sealed class SmartCacheMiddlewareOptions : ISmartCacheMiddlewareOptions
{
    public string? RootPath { get; set; }

    string ISmartCacheMiddlewareOptions.RootPath =>
        RootPath ?? throw new InvalidOperationException($"{nameof(RootPath)} is null");

    public string? GetPathSegment { get; set; }

    string ISmartCacheMiddlewareOptions.GetPathSegment => GetPathSegment ?? "/get";

    public string? CacheMissPathSegment { get; set; }

    string ISmartCacheMiddlewareOptions.CacheMissPathSegment => CacheMissPathSegment ?? "/cachemiss";

    public string? InvalidatePathSegment { get; set; }

    string ISmartCacheMiddlewareOptions.InvalidatePathSegment => InvalidatePathSegment ?? "/invalidate";
}
