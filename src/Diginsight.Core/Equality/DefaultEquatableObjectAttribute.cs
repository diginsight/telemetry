namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class DefaultEquatableObjectAttribute : Attribute, IDefaultEquatableDescriptor;
