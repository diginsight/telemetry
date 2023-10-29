using System.Reflection;

namespace Diginsight.Strings;

public interface ILogStringTypeContract : ILogStringTypeContractAccessor
{
    bool? Included { get; }

    ILogStringMemberContract? TryGet(MemberInfo member);
}
