namespace Diginsight.Strings;

internal sealed class ForbiddenLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryAsLogStringable(object obj)
    {
        Type type = obj.GetType();
        return type.IsForbidden() ? new NonLogStringable(type) : null;
    }
}
