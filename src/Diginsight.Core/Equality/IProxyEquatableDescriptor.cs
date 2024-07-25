namespace Diginsight.Equality;

public interface IProxyEquatableDescriptor
{
    Type ProxyType { get; }
    string? ProxyMember { get; }
    object?[] ProxyArgs { get; }
}
