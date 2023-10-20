using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public interface ILogStringProvider
{
    bool TryAsLoggable(object obj, [NotNullWhen(true)] out ILoggable? loggable);
}
