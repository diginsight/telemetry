namespace Diginsight.Stringify;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class NonStringifiableObjectAttribute : Attribute;
