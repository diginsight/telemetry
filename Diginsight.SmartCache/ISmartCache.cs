using Diginsight.SmartCache.Externalization;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public interface ISmartCache
{
    Task<T> GetAsync<T>(
        ICacheKey key,
        Func<CancellationToken, Task<T>> fetchAsync,
        SmartCacheOperationOptions? operationOptions = null,
        Type? callerType = null,
        CancellationToken cancellationToken = default
    );

    bool TryGetDirectFromMemory(ICacheKey key, [NotNullWhen(true)] out Type? type, out object? value);

    void Invalidate(IInvalidationRule invalidationRule);

    void Invalidate(InvalidationDescriptor descriptor);

    void AddExternalMiss(CacheMissDescriptor descriptor);
}
