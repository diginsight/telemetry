namespace Diginsight.Stringify;

/// <summary>
/// Represents a collection of stringify type contracts keyed by type.
/// </summary>
public sealed class StringifyTypeContractAccessor : IStringifyTypeContractAccessor
{
    private readonly IDictionary<Type, StringifyTypeContract> contracts = new Dictionary<Type, StringifyTypeContract>();

    /// <summary>
    /// Initializes a new instance of the <see cref="StringifyTypeContractAccessor" /> class.
    /// </summary>
    public StringifyTypeContractAccessor()
    {
        this.GetOrAdd<Exception>(
            static tc =>
            {
                tc
                    .GetOrAdd(static x => x.TargetSite, static mc => { mc.Included = false; })
                    .GetOrAdd(static x => x.Data, static mc => { mc.Included = false; })
                    .GetOrAdd(static x => x.HelpLink, static mc => { mc.Included = false; });
            }
        );
    }

    /// <summary>
    /// Gets an existing type contract or adds a new one.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The type contract.</returns>
    public StringifyTypeContract GetOrAdd(Type type)
    {
        if (contracts.TryGetValue(type, out StringifyTypeContract? contract))
        {
            return contract;
        }

        if (type.IsForbidden())
        {
            throw new ArgumentException($"Type {type.Name} is forbidden");
        }

        return contracts[type] = StringifyTypeContract.For(type);
    }

    /// <summary>
    /// Gets the type contract associated with the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The matching type contract if one exists; otherwise, <c>null</c>.</returns>
    public IStringifyTypeContract? TryGet(Type type)
    {
        return contracts.TryGetValue(type, out StringifyTypeContract? contract) ? contract : null;
    }
}
