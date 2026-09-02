namespace Diginsight.Stringify;

/// <summary>
/// Indicates that a type is eligible for memberwise stringification.
/// </summary>
/// <remarks>
/// This attribute is valid on classes, structs, and interfaces.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class StringifiableTypeAttribute : Attribute, IStringifiableTypeDescriptor;
