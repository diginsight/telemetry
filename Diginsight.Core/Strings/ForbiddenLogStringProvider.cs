namespace Diginsight.Strings;

internal sealed class ForbiddenLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryToLogStringable(object obj)
    {
        Type type = obj.GetType();
        return type.IsForbidden() ? new NonLogStringable(type) : null;
    }
}
