namespace Diginsight.Equality;

public abstract class EquatableObjectAttribute : Attribute, IEquatableObjectDescriptor
{
    public static readonly EquatableObjectAttribute Default = new DefaultEquatableObjectAttribute();

    public abstract EqualityBehavior Behavior { get; }
}
