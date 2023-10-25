using System.Reflection;

namespace Diginsight.Strings;

public interface ILogStringTypeContract
{
    bool? Included { get; }

    ILogStringMemberContract? TryGet(MemberInfo member);
}
