namespace Diginsight.Equality;

public abstract class EquatableObjectAttribute : Attribute
{
    public abstract IEquatableObjectDescriptor ToObjectDescriptor();
}
