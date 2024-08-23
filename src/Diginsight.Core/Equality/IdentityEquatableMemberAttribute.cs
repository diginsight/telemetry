namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IdentityEquatableMemberAttribute : EquatableMemberAttribute
{
    public override EqualityBehavior? Behavior => EqualityBehavior.Identity;
}
