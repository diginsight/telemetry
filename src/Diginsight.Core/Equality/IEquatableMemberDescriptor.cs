namespace Diginsight.Equality;

public interface IEquatableMemberDescriptor
{
    bool ByReference { get; }

    Type? ProxyType { get; }

    object[] ProxyArgs { get; }

    Type? ComparerType { get; }

    string? ComparerMember { get; }

    object[] ComparerArgs { get; }

    int? Order { get; }
}
