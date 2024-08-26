namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ForbiddenEquatableMemberAttribute : EquatableMemberAttribute
{
    protected override EqualityBehavior? Behavior => EqualityBehavior.Forbidden;
}
