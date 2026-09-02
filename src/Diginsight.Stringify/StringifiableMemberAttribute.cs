using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Stringify;

/// <summary>
/// Specifies member-level stringification metadata for a field or property.
/// </summary>
/// <remarks>
/// This attribute is valid on fields and properties.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StringifiableMemberAttribute : Attribute, IStringifiableMemberDescriptor
{
    private int order;
    private bool isOrderSet;

    /// <summary>
    /// Gets the output member name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the custom stringifier type.
    /// </summary>
    public Type? StringifierType { get; set; }

    /// <summary>
    /// Gets the custom stringifier constructor arguments.
    /// </summary>
    [field: MaybeNull]
    public object[] StringifierArgs
    {
        get => field ??= [ ];
        set;
    }

    /// <summary>
    /// Gets the member ordering value.
    /// </summary>
    public int Order
    {
        get => order;
        set
        {
            isOrderSet = true;
            order = value;
        }
    }

    int? IStringifiableMemberDescriptor.Order => isOrderSet ? order : null;
}
