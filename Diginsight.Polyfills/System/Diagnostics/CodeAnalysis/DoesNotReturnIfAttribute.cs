#if !(NET || NETSTANDARD2_1_OR_GREATER)
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DoesNotReturnIfAttribute : Attribute
{
    public bool ParameterValue { get; }

    public DoesNotReturnIfAttribute(bool parameterValue)
    {
        ParameterValue = parameterValue;
    }
}
#endif
