namespace Diginsight.Stringify;

/// <summary>
/// Represents a stringifier that renders objects through a custom memberwise stringify contract.
/// </summary>
public sealed class CustomMemberwiseStringifier : IStringifier
{
    private readonly IReflectionStringifyHelper helper;
    private readonly IStringifyTypeContract contract;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomMemberwiseStringifier" /> class.
    /// </summary>
    /// <param name="helper">The reflection stringify helper.</param>
    /// <param name="contract">The type contract.</param>
    public CustomMemberwiseStringifier(
        IReflectionStringifyHelper helper,
        IStringifyTypeContract contract
    )
    {
        this.helper = helper;
        this.contract = contract;
    }

    /// <inheritdoc />
    public IStringifiable TryStringify(object obj) => new MemberwiseStringifiable(obj, helper, contract);
}
