namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EquatableMemberAttribute
    : Attribute, IEquatableMemberDescriptor, IEquatableObjectDescriptor
{
    private int order;
    private bool isOrderSet;

    public virtual EqualityBehavior? Behavior => null;

    EqualityBehavior IEquatableObjectDescriptor.Behavior => Behavior ?? throw new InvalidOperationException($"{nameof(Behavior)} is unset");

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

    IEquatableObjectDescriptor? IEquatableMemberDescriptor.TryToObjectDescriptor() => Behavior is null ? null : this;

    IEquatableMemberDescriptor IEquatableObjectDescriptor.ToMemberDescriptor() => this;
}
