#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class AllowNullAttribute : Attribute;
#endif
