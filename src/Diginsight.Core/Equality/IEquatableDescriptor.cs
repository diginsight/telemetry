namespace Diginsight.Equality;

public interface IEquatableDescriptor
{
    EqualityMode Mode { get; }

    Type? ProxyType { get; }

    object[] ProxyArgs { get; }
}
