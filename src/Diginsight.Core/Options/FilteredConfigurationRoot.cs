using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

public sealed class FilteredConfigurationRoot : FilteredConfiguration, IConfigurationRoot
{
    private readonly IConfigurationRoot underlying;

    public IEnumerable<IConfigurationProvider> Providers => underlying.Providers;

    internal FilteredConfigurationRoot(IConfigurationRoot underlying, Type @class)
        : base(underlying, @class)
    {
        this.underlying = underlying;
    }

    public void Reload() => underlying.Reload();
}
