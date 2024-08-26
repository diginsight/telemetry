namespace Diginsight.Equality;

internal sealed record ProxyEquatableDescriptor(Type ProxyType, string? ProxyMember, object?[] ProxyArgs, int? Order)
    : EquatableDescriptor(EqualityBehavior.Proxy, Order), IProxyEquatableObjectDescriptor, IProxyEquatableMemberDescriptor;
