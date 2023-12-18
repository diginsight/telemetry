using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public interface ICacheKeyService
{
    bool TryToKey(object? obj, [NotNullWhen(true)] out ICacheKey? key);
}
