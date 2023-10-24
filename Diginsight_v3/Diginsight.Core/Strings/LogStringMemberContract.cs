namespace Diginsight.Strings;

public sealed class LogStringMemberContract
{
    public static readonly LogStringMemberContract Empty = new (null, null, null, false);

    public bool? Included { get; }
    public string? Name { get; }
    public Type? ProviderType { get; }

    public LogStringMemberContract(bool? included, string? name, Type? providerType)
        : this(included, name, providerType, true) { }

    internal LogStringMemberContract(bool? included, string? name, Type? providerType, bool validateProvider)
    {
        if (validateProvider && providerType is not null && !typeof(ILogStringProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException($"Type '{providerType.Name}' is not assignable to {nameof(ILogStringProvider)}");
        }

        Included = included;
        Name = name;
        ProviderType = providerType;
    }
}
