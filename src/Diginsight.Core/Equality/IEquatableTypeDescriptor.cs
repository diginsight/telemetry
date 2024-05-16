namespace Diginsight.Equality;

public interface IEquatableTypeDescriptor
{
    bool ByReference { get; }

    bool Loose { get; }

    Type? ProxyType { get; }

    object[] ProxyArgs { get; }
}
