namespace Diginsight.Stringify;

public sealed class CustomMemberwiseStringifier : IStringifier
{
    private readonly IReflectionStringifyHelper helper;
    private readonly IStringifyTypeContract contract;

    public CustomMemberwiseStringifier(
        IReflectionStringifyHelper helper,
        IStringifyTypeContract contract
    )
    {
        this.helper = helper;
        this.contract = contract;
    }

    public IStringifiable TryStringify(object obj) => new MemberwiseStringifiable(obj, helper, contract);
}
