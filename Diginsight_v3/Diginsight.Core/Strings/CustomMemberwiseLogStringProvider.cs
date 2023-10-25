using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public sealed class CustomMemberwiseLogStringProvider : ILogStringProvider
{
    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        throw new NotImplementedException();
    }
}
