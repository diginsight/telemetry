namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IdentityEquatableMemberAttribute : EquatableMemberAttribute, IIdentityEquatableMemberDescriptor;
