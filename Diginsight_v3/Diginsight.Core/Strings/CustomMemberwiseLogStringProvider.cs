using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public sealed class CustomMemberwiseLogStringProvider : ILogStringProvider
{
    private readonly IReflectionLogStringHelper helper;
    private readonly ILogStringTypeContract contract;

    public CustomMemberwiseLogStringProvider(
        IReflectionLogStringHelper helper,
        ILogStringTypeContract contract
    )
    {
        this.helper = helper;
        this.contract = contract;
    }

    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        logStringable = new MemberwiseLogStringable(obj, helper, contract);
        return true;
    }
}
