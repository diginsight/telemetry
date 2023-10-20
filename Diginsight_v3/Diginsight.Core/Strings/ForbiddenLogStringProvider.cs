using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

internal sealed class ForbiddenLogStringProvider : ILogStringProvider
{
    public bool TryAsLoggable(object obj, [NotNullWhen(true)] out ILoggable? loggable)
    {
        Type type = obj.GetType();
        if (type.IsForbidden())
        {
            loggable = new NonLoggable(type);
            return true;
        }

        loggable = null;
        return false;
    }
}
