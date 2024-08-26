namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IdentityEquatableMemberAttribute : EquatableMemberAttribute
{
    protected override EqualityBehavior? Behavior => EqualityBehavior.Identity;
}
