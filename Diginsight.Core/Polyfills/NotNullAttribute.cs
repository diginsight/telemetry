#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class NotNullAttribute : Attribute;
#endif
