using System.Reflection;

namespace Diginsight.Equality;

public interface IEqualityTypeContract : IEqualityContract, IEquatableObjectDescriptor
{
    IEqualityMemberContract? TryGet(MemberInfo member);
}
