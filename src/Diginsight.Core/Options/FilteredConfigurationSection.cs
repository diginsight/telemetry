using Microsoft.Extensions.Configuration;

namespace Diginsight.Options;

/// <summary>
/// Represents an <see cref="IConfigurationSection" /> whose keys are filtered for a specific class.
/// </summary>
public sealed class FilteredConfigurationSection : FilteredConfiguration, IConfigurationSection
{
    private readonly IConfigurationSection underlying;

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public string Path { get; }

    /// <inheritdoc />
    public string? Value
    {
        get => underlying.Value;
        set => underlying.Value = value;
    }

    internal FilteredConfigurationSection(IConfigurationSection underlying, Type @class, string? virtualPath = null)
        : base(underlying, @class, ConfigurationPath.GetParentPath(virtualPath ??= underlying.Path) + ConfigurationPath.KeyDelimiter)
    {
        this.underlying = underlying;

        Key = ConfigurationPath.GetSectionKey(virtualPath);
        Path = virtualPath;
    }
}
