using System.Reflection;

namespace Diginsight.Equality;

public interface IEqualityTypeContract : IEqualityContract
{
    IEqualityMemberContract? TryGet(MemberInfo member);
}
