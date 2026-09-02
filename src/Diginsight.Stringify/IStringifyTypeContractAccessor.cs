namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for accessing stringify type contracts.
/// </summary>
public interface IStringifyTypeContractAccessor
{
    /// <summary>
    /// Gets the type contract associated with the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The matching stringify contract if one exists; otherwise, <c>null</c>.</returns>
    IStringifyTypeContract? TryGet(Type type);
}
