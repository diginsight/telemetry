#if !(NET || NETSTANDARD2_1_OR_GREATER)
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class NotNullWhenAttribute : Attribute
{
    public bool ReturnValue { get; }

    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }
}
#endif
