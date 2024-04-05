#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MaybeNullWhenAttribute : Attribute
{
    public bool ReturnValue { get; }

    public MaybeNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }
}
#endif
