using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

internal sealed class ForbiddenLogStringProvider : ILogStringProvider
{
    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        Type type = obj.GetType();
        if (type.IsForbidden())
        {
            logStringable = new NonLogStringable(type);
            return true;
        }

        logStringable = null;
        return false;
    }
}
