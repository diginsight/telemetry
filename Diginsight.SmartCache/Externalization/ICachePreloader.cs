namespace Diginsight.SmartCache.Externalization;

public interface ICachePreloader
{
    Task PreloadAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync);
}
