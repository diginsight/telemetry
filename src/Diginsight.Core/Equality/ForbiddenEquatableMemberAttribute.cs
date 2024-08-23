namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ForbiddenEquatableMemberAttribute : EquatableMemberAttribute
{
    public override EqualityBehavior? Behavior => EqualityBehavior.Forbidden;
}
