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

    public ILogStringable TryToLogStringable(object obj) => new MemberwiseLogStringable(obj, helper, contract);
}
