using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Stringify;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StringifiableMemberAttribute : Attribute, IStringifiableMemberDescriptor
{
    private int order;
    private bool isOrderSet;

    public string? Name { get; set; }

    public Type? StringifierType { get; set; }

    [field: MaybeNull]
    public object[] StringifierArgs
    {
        get => field ??= [ ];
        set;
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
