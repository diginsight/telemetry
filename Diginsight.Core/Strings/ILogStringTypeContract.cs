using System.Reflection;

namespace Diginsight.Strings;

public interface ILogStringTypeContract : ILogStringTypeContractAccessor, ILogStringableTypeDescriptor
{
    bool? Included { get; }

    ILogStringMemberContract? TryGet(MemberInfo member);
}
