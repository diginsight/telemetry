namespace Diginsight.SmartCache.Externalization;

public abstract class PassiveCacheLocation : CacheLocation
{
    protected PassiveCacheLocation(string id)
        : base(id) { }

    public void WriteAndForget(CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration, Func<Task> notifyMissAsync)
    {
        TaskUtils.RunAndForget(
            async () =>
            {
                if (await TryWriteAsync(keyHolder, entry, expiration))
                {
                    await notifyMissAsync();
                }
            }
        );
    }

    protected abstract Task<bool> TryWriteAsync(CacheKeyHolder keyHolder, IValueEntry entry, Expiration expiration);

    public void DeleteAndForget(CacheKeyHolder keyHolder)
    {
        TaskUtils.RunAndForget(() => DeleteAsync(keyHolder));
    }

    protected abstract Task DeleteAsync(CacheKeyHolder keyHolder);
}
