namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class AttributedEquatableObjectAttribute : EquatableObjectAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Attributed;
}
