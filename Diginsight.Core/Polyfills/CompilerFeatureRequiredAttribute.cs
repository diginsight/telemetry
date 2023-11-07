#if !NET7_0_OR_GREATER
// ReSharper disable once CheckNamespace

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public sealed class CompilerFeatureRequiredAttribute : Attribute
{
    public const string RefStructs = "RefStructs";

    public const string RequiredMembers = "RequiredMembers";

    public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

    public string FeatureName { get; }

    public bool IsOptional { get; init; }
}
#endif
