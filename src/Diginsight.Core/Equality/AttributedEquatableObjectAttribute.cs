namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class AttributedEquatableObjectAttribute : Attribute, IAttributedEquatableDescriptor;
