namespace Diginsight.Equality;

public interface IEquatableObjectDescriptor : IEquatableDescriptor
{
    EqualityBehavior Behavior { get; }

    IEquatableMemberDescriptor ToMemberDescriptor();
}
