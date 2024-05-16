namespace Diginsight.Equality;

public interface IEqualityMemberContract : IEquatableMemberDescriptor
{
    bool? Included { get; }
}
