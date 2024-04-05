namespace Diginsight.SmartCache.Externalization.Http;

public interface ISmartCacheHttpOptions
{
    bool UseHttps { get; }

    string RootPath { get; }

    string GetPathSegment { get; }
    string CacheMissPathSegment { get; }
    string InvalidatePathSegment { get; }
}
