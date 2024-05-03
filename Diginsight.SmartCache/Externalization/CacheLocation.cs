namespace Diginsight.SmartCache.Externalization;

public abstract class CacheLocation
{
    public string Id { get; }

    public abstract KeyValuePair<string, object?> MetricTag { get; }

    private protected CacheLocation(string id)
    {
        Id = id;
    }

    public abstract Task<CacheLocationOutput<TValue>?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTimeOffset minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    );
}
