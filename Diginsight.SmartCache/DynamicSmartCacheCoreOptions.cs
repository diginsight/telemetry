using Diginsight.CAOptions;

namespace Diginsight.SmartCache;

internal sealed class DynamicSmartCacheCoreOptions : IDynamicSmartCacheCoreOptions, IDynamicallyPostConfigurable
{
    public DateTime? MinimumCreationDate { get; set; }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    object IDynamicallyPostConfigurable.MakeFiller() => this;
#endif
}
