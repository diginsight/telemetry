namespace Diginsight.Stringify;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class NonStringifiableMemberAttribute : Attribute;
