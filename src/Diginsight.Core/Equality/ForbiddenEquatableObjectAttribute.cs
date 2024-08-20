namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ForbiddenEquatableObjectAttribute : EquatableObjectAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Forbidden;
}
