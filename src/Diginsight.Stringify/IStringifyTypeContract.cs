using System.Reflection;

namespace Diginsight.Stringify;

public interface IStringifyTypeContract : IStringifyTypeContractAccessor, IStringifiableTypeDescriptor
{
    bool? Included { get; }

    IStringifyMemberContract? TryGet(MemberInfo member);
}
