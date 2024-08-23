namespace Diginsight.Equality;

public interface IEquatableObjectDescriptor : IEquatableDescriptor
{
    IEquatableMemberDescriptor ToMemberDescriptor();
}
