namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class EquatableMemberAttribute : Attribute, IEquatableMemberDescriptor
{
    private int order;
    private bool isOrderSet;

    public abstract EqualityBehavior Behavior { get; }

    public int Order
    {
        get => order;
        set
        {
            isOrderSet = true;
            order = value;
        }
    }

    int? IEquatableMemberDescriptor.Order => isOrderSet ? order : null;

    public void UnsetOrder()
    {
        isOrderSet = false;
        order = 0;
    }
}
