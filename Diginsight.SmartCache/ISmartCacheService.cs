using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public interface ISmartCacheService
{
    Task<T> GetAsync<T>(ICacheKey key, Func<Task<T>> fetchAsync, ISmartCacheOperationOptions? cacheOperationOptions = null);

    bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value);

    void Invalidate(IInvalidationRule invalidationRule);

    void Invalidate(InvalidationDescriptor descriptor);

    void AddExternalMiss(CacheMissDescriptor descriptor);
}
