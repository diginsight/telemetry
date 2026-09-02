namespace Diginsight.Stringify;

/// <summary>
/// Indicates that a field or property is excluded from memberwise stringification.
/// </summary>
/// <remarks>
/// This attribute is valid on fields and properties.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class NonStringifiableMemberAttribute : Attribute;
