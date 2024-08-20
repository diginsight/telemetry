namespace Diginsight.Equality;

public abstract class EquatableObjectAttribute : Attribute, IEquatableObjectDescriptor
{
    public abstract EqualityBehavior Behavior { get; }
}
