namespace Diginsight.Equality;

internal record EquatableDescriptor(EqualityBehavior? Behavior, int? Order)
    : IEquatableObjectDescriptor, IEquatableMemberDescriptor
{
    EqualityBehavior IEquatableObjectDescriptor.Behavior => Behavior ?? throw new InvalidOperationException($"{nameof(Behavior)} is unset");

    public EquatableDescriptor(EqualityBehavior behavior)
        : this(behavior, null) { }

    public IEquatableObjectDescriptor? TryToObjectDescriptor() => Behavior is null ? null : this with { Order = null };

    public IEquatableMemberDescriptor ToMemberDescriptor() => this;
}
