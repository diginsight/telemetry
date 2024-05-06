#if !(NET || NETSTANDARD2_1_OR_GREATER)
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class NotNullAttribute : Attribute;
#endif
