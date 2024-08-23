namespace Diginsight.Equality;

public interface IProxyEquatableDescriptor : IEquatableDescriptor
{
    Type ProxyType { get; }
    string? ProxyMember { get; }
    object?[] ProxyArgs { get; }
}
