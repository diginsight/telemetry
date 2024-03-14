using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public sealed class FilteredConfigurationRoot<TClass> : FilteredConfiguration<TClass>, IConfigurationRoot
{
    private readonly IConfigurationRoot underlying;

    public IEnumerable<IConfigurationProvider> Providers => underlying.Providers;

    internal FilteredConfigurationRoot(IConfigurationRoot underlying)
        : base(underlying)
    {
        this.underlying = underlying;
    }

    public void Reload() => underlying.Reload();
}
