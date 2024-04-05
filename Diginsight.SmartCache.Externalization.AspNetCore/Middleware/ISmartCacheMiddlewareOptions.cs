namespace Diginsight.SmartCache.Externalization.Middleware;

public interface ISmartCacheMiddlewareOptions
{
    string RootPath { get; }

    string GetPathSegment { get; }
    string CacheMissPathSegment { get; }
    string InvalidatePathSegment { get; }
}
