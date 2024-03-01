namespace Diginsight.SmartCache.Externalization;

public abstract class CacheEventNotifier
{
    public void NotifyCacheMissAndForget(CachePayloadHolder<CacheMissDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => NotifyCacheMissAsync(descriptorHolder));
    }

    protected abstract Task NotifyCacheMissAsync(CachePayloadHolder<CacheMissDescriptor> descriptorHolder);

    public void NotifyInvalidationAndForget(CachePayloadHolder<InvalidationDescriptor> descriptorHolder)
    {
        _ = Task.Run(() => NotifyInvalidationAsync(descriptorHolder));
    }

    protected abstract Task NotifyInvalidationAsync(CachePayloadHolder<InvalidationDescriptor> descriptorHolder);
}
