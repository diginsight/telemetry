namespace Diginsight.SmartCache;

public abstract class CacheLocation
{
    public string Id { get; }

    public abstract KeyValuePair<string, object?> MetricTag { get; }

    protected CacheLocation(string id)
    {
        Id = id;
    }

    public abstract Task<(TValue Value, long SerializedSize, double RelativeLatency)?> GetAsync<TValue>(
        CacheKeyHolder keyHolder, DateTime minimumCreationDate, Action markInvalid, CancellationToken cancellationToken
    );
}
