namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class DefaultEquatableObjectAttribute : EquatableObjectAttribute
{
    public override IEquatableObjectDescriptor ToObjectDescriptor() => new EquatableDescriptor(EqualityBehavior.Default);
}
