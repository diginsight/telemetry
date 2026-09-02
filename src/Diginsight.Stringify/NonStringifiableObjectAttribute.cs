namespace Diginsight.Stringify;

/// <summary>
/// Indicates that a type is excluded from memberwise stringification.
/// </summary>
/// <remarks>
/// This attribute is valid on classes, structs, and interfaces.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class NonStringifiableObjectAttribute : Attribute;
