namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class IdentityEquatableObjectAttribute : EquatableObjectAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Identity;
}
