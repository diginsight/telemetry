namespace Diginsight.SmartCache;

public interface ICachable
{
    ToKeyResult ToKey(ICacheKeyService service);
}
