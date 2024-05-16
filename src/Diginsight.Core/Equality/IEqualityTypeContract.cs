using System.Reflection;

namespace Diginsight.Equality;

public interface IEqualityTypeContract : IEqualityTypeContractAccessor, IEquatableTypeDescriptor
{
    bool? Included { get; }

    IEqualityMemberContract? TryGet(MemberInfo member);
}
