namespace Diginsight.SmartCache;

public interface ICacheKeyService
{
    ToKeyResult ToKey(object? obj);
}
