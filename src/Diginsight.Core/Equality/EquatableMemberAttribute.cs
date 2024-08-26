namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EquatableMemberAttribute : Attribute
{
    private int order;
    private bool isOrderSet;

    protected virtual EqualityBehavior? Behavior => null;

    public int Order
    {
        get => order;
        set
        {
            isOrderSet = true;
            order = value;
        }
    }

    protected int? OrderCore => isOrderSet ? order : null;

    public void UnsetOrder()
    {
        isOrderSet = false;
        order = 0;
    }

    public virtual IEquatableMemberDescriptor ToMemberDescriptor() => new EquatableDescriptor(Behavior, OrderCore);
}
