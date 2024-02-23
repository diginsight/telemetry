namespace Diginsight.SmartCache;

public abstract class PassiveCacheLocation : CacheLocation
{
    protected PassiveCacheLocation(string id)
        : base(id) { }

    public void WriteAndForget(CacheKeyHolder keyHolder, IValueEntry entry, TimeSpan? expiration, Func<Task> publishMissAsync)
    {
        _ = Task.Run(() => WriteAsync(keyHolder, entry, expiration, publishMissAsync));
    }

    protected abstract Task WriteAsync(CacheKeyHolder keyHolder, IValueEntry entry, TimeSpan? expiration, Func<Task> publishMissAsync);

    public void DeleteAndForget(CacheKeyHolder keyHolder)
    {
        _ = Task.Run(() => DeleteAsync(keyHolder));
    }

    protected abstract Task DeleteAsync(CacheKeyHolder keyHolder);
}
