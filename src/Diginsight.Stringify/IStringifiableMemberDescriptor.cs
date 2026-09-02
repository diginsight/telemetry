namespace Diginsight.Stringify;

/// <summary>
/// Represents descriptor metadata for a stringifiable member.
/// </summary>
public interface IStringifiableMemberDescriptor : IStringifiableDescriptor
{
    /// <summary>
    /// Gets the output member name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the custom stringifier type.
    /// </summary>
    Type? StringifierType { get; }

    /// <summary>
    /// Gets the custom stringifier constructor arguments.
    /// </summary>
    object[] StringifierArgs { get; }

    /// <summary>
    /// Gets the member ordering value.
    /// </summary>
    int? Order { get; }
}
