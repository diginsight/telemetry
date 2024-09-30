namespace Diginsight.Stringify;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StringifiableMemberAttribute : Attribute, IStringifiableMemberDescriptor
{
    private object[]? stringifierArgs;
    private int order;
    private bool isOrderSet;

    public string? Name { get; set; }

    public Type? StringifierType { get; set; }

    public object[] StringifierArgs
    {
        get => stringifierArgs ??= [ ];
        set => stringifierArgs = value;
    }

    public int Order
    {
        get => order;
        set
        {
            isOrderSet = true;
            order = value;
        }
    }

    int? IStringifiableMemberDescriptor.Order => isOrderSet ? order : null;
}
