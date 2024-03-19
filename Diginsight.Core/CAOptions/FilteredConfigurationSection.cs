using Microsoft.Extensions.Configuration;

namespace Diginsight.CAOptions;

public sealed class FilteredConfigurationSection : FilteredConfiguration, IConfigurationSection
{
    private readonly IConfigurationSection underlying;

    public string Key { get; }

    public string Path { get; }

    public string? Value
    {
        get => underlying.Value;
        set => underlying.Value = value;
    }

    internal FilteredConfigurationSection(IConfigurationSection underlying, Type @class, string? virtualPath = null)
        : base(underlying, @class, ConfigurationPath.GetParentPath(virtualPath) + ConfigurationPath.KeyDelimiter)
    {
        this.underlying = underlying;

        virtualPath ??= underlying.Path;
        Key = ConfigurationPath.GetSectionKey(virtualPath);
        Path = virtualPath;
    }
}
