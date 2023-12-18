#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public bool ReturnValue { get; }

    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }
}
#endif
