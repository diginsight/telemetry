namespace Diginsight.Strings;

public interface ILogStringMemberContract : ILogStringableMemberDescriptor
{
    bool? Included { get; }
}
