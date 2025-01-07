namespace Diginsight.Stringify;

public interface IStringifiableMemberDescriptor : IStringifiableDescriptor
{
    string? Name { get; }

    Type? StringifierType { get; }

    object[] StringifierArgs { get; }

    int? Order { get; }
}
