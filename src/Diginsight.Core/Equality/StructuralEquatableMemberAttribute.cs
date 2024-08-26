namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StructuralEquatableMemberAttribute : EquatableMemberAttribute
{
    protected override EqualityBehavior? Behavior => EqualityBehavior.Structural;
}
