namespace Diginsight.SmartCache.Externalization;

public abstract class CacheCompanion : CacheLocation
{
    public override KeyValuePair<string, object?> MetricTag => SmartCacheMetrics.Tags.Type.Distributed;

    protected CacheCompanion(string id)
        : base(id) { }

    public void PublishCacheMissAndForget(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => PublishCacheMissAsync(descriptorHolder));
    }

    protected abstract Task PublishCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder);

    public void PublishInvalidationAndForget(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => PublishInvalidationAsync(descriptorHolder));
    }

    protected abstract Task PublishInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder);
}
