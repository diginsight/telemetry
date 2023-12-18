namespace Diginsight.SmartCache;

[CacheInterchangeName("IR")]
public interface IInvalidationRule
{
    InvalidationReason Reason { get; }
}
