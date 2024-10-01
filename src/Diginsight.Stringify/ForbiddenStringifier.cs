namespace Diginsight.Stringify;

internal sealed class ForbiddenStringifier : IStringifier
{
    public IStringifiable? TryStringify(object obj)
    {
        Type type = obj.GetType();
        return type.IsForbidden() ? new NonStringifiable(type) : null;
    }
}
