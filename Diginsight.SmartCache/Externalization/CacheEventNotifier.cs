namespace Diginsight.SmartCache.Externalization;

public abstract class CacheEventNotifier
{
    public void NotifyCacheMissAndForget(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        TaskUtils.RunAndForget(() => NotifyCacheMissAsync(descriptorHolder));
    }

    protected abstract Task NotifyCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder);

    public void NotifyInvalidationAndForget(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        TaskUtils.RunAndForget(() => NotifyInvalidationAsync(descriptorHolder));
    }

    protected abstract Task NotifyInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder);
}
