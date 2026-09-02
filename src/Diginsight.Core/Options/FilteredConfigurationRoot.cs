using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

/// <summary>
/// Represents an <see cref="IConfigurationRoot" /> whose keys are filtered for a specific class.
/// </summary>
public sealed class FilteredConfigurationRoot : FilteredConfiguration, IConfigurationRoot
{
    private readonly IConfigurationRoot underlying;

    /// <inheritdoc />
    public IEnumerable<IConfigurationProvider> Providers => underlying.Providers;

    internal FilteredConfigurationRoot(IConfigurationRoot underlying, Type @class)
        : base(underlying, @class)
    {
        this.underlying = underlying;
    }

    /// <inheritdoc />
    public void Reload() => underlying.Reload();
}
