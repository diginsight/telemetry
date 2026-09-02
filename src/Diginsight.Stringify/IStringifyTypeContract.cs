using System.Reflection;

namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for type-level stringify contract configuration.
/// </summary>
public interface IStringifyTypeContract : IStringifyTypeContractAccessor, IStringifiableTypeDescriptor
{
    /// <summary>
    /// Gets a value indicating whether the type is included in stringification.
    /// </summary>
    bool? Included { get; }

    /// <summary>
    /// Gets the member contract associated with the specified member.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <returns>The matching stringify contract if one exists; otherwise, <c>null</c>.</returns>
    IStringifyMemberContract? TryGet(MemberInfo member);
}
