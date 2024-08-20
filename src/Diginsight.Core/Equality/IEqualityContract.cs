namespace Diginsight.Equality;

public interface IEqualityContract : IEquatableDescriptor
{
    new EqualityBehavior? Behavior { get; }

    IComparerEquatableDescriptor? ComparerDescriptor { get; }

    IProxyEquatableDescriptor? ProxyDescriptor { get; }
}
