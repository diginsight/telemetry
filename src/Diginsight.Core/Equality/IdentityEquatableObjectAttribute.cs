namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class IdentityEquatableObjectAttribute : Attribute, IIdentityEquatableDescriptor;
