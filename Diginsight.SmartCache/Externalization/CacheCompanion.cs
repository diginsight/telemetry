namespace Diginsight.SmartCache.Externalization;

public abstract class CacheCompanion : CacheLocation
{
    public override KeyValuePair<string, object?> MetricTag => SmartCacheMetrics.Tags.Type.Distributed;

    protected CacheCompanion(string id)
        : base(id) { }

    public void PublishCacheMissAndForget(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => PublishCacheMissAndForgetAsync(descriptorHolder));
    }

    protected abstract Task PublishCacheMissAndForgetAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder);

    public void PublishInvalidationAndForget(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => PublishInvalidationAndForgetAsync(descriptorHolder));
    }

    protected abstract Task PublishInvalidationAndForgetAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder);
}
