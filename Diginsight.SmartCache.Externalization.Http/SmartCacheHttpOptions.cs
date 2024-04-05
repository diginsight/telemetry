namespace Diginsight.SmartCache.Externalization.Http;

public sealed class SmartCacheHttpOptions : ISmartCacheHttpOptions
{
    public bool UseHttps { get; set; }

    public string? RootPath { get; set; }

    string ISmartCacheHttpOptions.RootPath =>
        RootPath ?? throw new InvalidOperationException($"{nameof(RootPath)} is null");

    public string? GetPathSegment { get; set; }

    string ISmartCacheHttpOptions.GetPathSegment => GetPathSegment ?? "/get";

    public string? CacheMissPathSegment { get; set; }

    string ISmartCacheHttpOptions.CacheMissPathSegment => CacheMissPathSegment ?? "/cachemiss";

    public string? InvalidatePathSegment { get; set; }

    string ISmartCacheHttpOptions.InvalidatePathSegment => InvalidatePathSegment ?? "/invalidate";
}
