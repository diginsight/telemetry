namespace Diginsight.Equality;

public interface IEqualityContract
{
    IAttributedEquatableDescriptor? AttributedDescriptor { get; }

    IDefaultEquatableDescriptor? DefaultDescriptor { get; }

    IIdentityEquatableDescriptor? IdentityDescriptor { get; }

    IProxyEquatableDescriptor? ProxyDescriptor { get; }
}
