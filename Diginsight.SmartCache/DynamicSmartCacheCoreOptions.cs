using Diginsight.CAOptions;

namespace Diginsight.SmartCache;

internal sealed class DynamicSmartCacheCoreOptions : IDynamicSmartCacheCoreOptions, IDynamicallyPostConfigurable
{
    public DateTime? MinimumCreationDate { get; set; }

#if !(NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER)
    public object MakeFiller() => this;
#endif
}
