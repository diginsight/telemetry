namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AttributedEquatableMemberAttribute : EquatableMemberAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Attributed;
}
