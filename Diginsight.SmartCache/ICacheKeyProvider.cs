using System.Diagnostics.CodeAnalysis;

namespace Diginsight.SmartCache;

public interface ICacheKeyProvider
{
    bool TryToKey(ICacheKeyService service, object? obj, [NotNullWhen(true)] out ICacheKey? key);
}
