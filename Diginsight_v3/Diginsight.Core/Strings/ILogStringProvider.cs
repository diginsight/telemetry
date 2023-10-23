using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public interface ILogStringProvider
{
    bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable);
}
