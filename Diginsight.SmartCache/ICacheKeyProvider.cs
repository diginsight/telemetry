namespace Diginsight.SmartCache;

public interface ICacheKeyProvider
{
    ToKeyResult ToKey(ICacheKeyService service, object? obj);
}
