namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StructuralEquatableMemberAttribute : EquatableMemberAttribute
{
    public override EqualityBehavior? Behavior => EqualityBehavior.Structural;
}
