namespace Diginsight.SmartCache;

public interface ICachable
{
    ToKeyResult GetKey(ICacheKeyService service);
}
