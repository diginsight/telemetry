namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class StructuralEquatableObjectAttribute : EquatableObjectAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Structural;
}
