namespace Diginsight.Stringify;

public interface IStringifyMemberContract : IStringifiableMemberDescriptor
{
    bool? Included { get; }
}
