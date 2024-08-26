namespace Diginsight.Equality;

public abstract class EquatableObjectAttribute : Attribute, IEquatableObjectDescriptor, IEquatableMemberDescriptor
{
    public abstract EqualityBehavior Behavior { get; }

    EqualityBehavior? IEquatableMemberDescriptor.Behavior => Behavior;

    int? IEquatableMemberDescriptor.Order => null;

    IEquatableMemberDescriptor IEquatableObjectDescriptor.ToMemberDescriptor() => this;

    IEquatableObjectDescriptor? IEquatableMemberDescriptor.TryToObjectDescriptor() => this;
}
