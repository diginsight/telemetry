namespace Diginsight.Stringify;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class StringifiableTypeAttribute : Attribute, IStringifiableTypeDescriptor;
