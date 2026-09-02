namespace Diginsight.Stringify;

/// <summary>
/// Represents an interface for member-level stringify contract configuration.
/// </summary>
public interface IStringifyMemberContract : IStringifiableMemberDescriptor
{
    /// <summary>
    /// Gets a value indicating whether the member is included in stringification.
    /// </summary>
    bool? Included { get; }
}
