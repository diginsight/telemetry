namespace Diginsight.Equality;

public interface IEqualityMemberContract : IEqualityContract
{
    IEquatableMemberDescriptor ToDescriptor();
}
