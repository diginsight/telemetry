namespace Diginsight.SmartCache;

public interface ICachable
{
    ICacheKey GetKey(ICacheKeyService service);
}
