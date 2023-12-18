namespace Diginsight.SmartCache;

public interface ISmartCacheMiddlewareOptions
{
    string? RootPath { get; }

    string? GetPathSegment { get; }

    string? CacheMissPathSegment { get; }

    string? InvalidatePathSegment { get; }
}
