namespace Diginsight.Equality;

public abstract class EquatableObjectAttribute : Attribute, IEquatableObjectDescriptor, IEquatableMemberDescriptor
{
    public abstract EqualityBehavior Behavior { get; }

    int? IEquatableMemberDescriptor.Order => null;

    IEquatableMemberDescriptor IEquatableObjectDescriptor.ToMemberDescriptor() => this;
}
