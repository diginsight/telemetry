namespace Diginsight.SmartCache.Externalization;

public abstract class PassiveCacheLocation : CacheLocation
{
    protected PassiveCacheLocation(string id)
        : base(id) { }

    public void WriteAndForget(CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration, Func<Task> notifyMissAsync)
    {
        _ = Task.Run(() => WriteAsync(keyHolder, entry, expiration, notifyMissAsync));
    }

    protected abstract Task WriteAsync(CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration, Func<Task> notifyMissAsync);

    public void DeleteAndForget(CacheKeyHolder keyHolder)
    {
        _ = Task.Run(() => DeleteAsync(keyHolder));
    }

    protected abstract Task DeleteAsync(CacheKeyHolder keyHolder);
}
