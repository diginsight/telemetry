namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DefaultEquatableMemberAttribute : EquatableMemberAttribute
{
    protected override EqualityBehavior? Behavior => EqualityBehavior.Default;
}
