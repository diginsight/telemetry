namespace Diginsight.Equality;

public interface IEquatableMemberDescriptor : IEquatableDescriptor
{
    Type? ComparerType { get; }

    string? ComparerMember { get; }

    object[] ComparerArgs { get; }

    int? Order { get; }
}
