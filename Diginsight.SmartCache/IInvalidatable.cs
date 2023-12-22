namespace Diginsight.SmartCache;

public interface IInvalidatable
{
    bool IsInvalidatedBy(IInvalidationRule invalidationRule, out Func<Task>? invalidationCallback);
}
