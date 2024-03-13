#if EXPERIMENT_FILTERED_CONFIGURATION
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

internal sealed class FilteredConfigurationSection : IConfigurationSection
{
    private readonly FilteredConfigurationRoot root;
    private readonly string?[] actualPathSegments;

    public string Key { get; }

    public string Path { get; }

    public string? Value
    {
        get => root.CoreGetValue(Path, actualPathSegments);
        set => root.CoreSetValue(Path, actualPathSegments, value);
    }

    public string? this[string key]
    {
        get => root.CoreGetValue($"{Path}{ConfigurationPath.KeyDelimiter}{key}", Expand(actualPathSegments, key));
        set => root.CoreSetValue($"{Path}{ConfigurationPath.KeyDelimiter}{key}", Expand(actualPathSegments, key), value);
    }

    public IConfigurationSection? Underlying { get; }

    public FilteredConfigurationSection(
        FilteredConfigurationRoot root, string virtualPath, string?[] actualPathSegments, IConfigurationSection? underlying
    )
    {
        this.root = root;

        Key = ConfigurationPath.GetSectionKey(virtualPath);
        Path = virtualPath;

        this.actualPathSegments = actualPathSegments;
        Underlying = underlying;
    }

    public IConfigurationSection GetSection(string key)
    {
        return new FilteredConfigurationSection(
            root, $"{Path}{ConfigurationPath.KeyDelimiter}{key}", Expand(actualPathSegments, key), null
        );
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return root.CoreGetChildren(Path, actualPathSegments);
    }

    public IChangeToken GetReloadToken() => root.GetReloadToken();

    private static string?[] Expand(string?[] actualPathSegments, string key)
    {
        string?[] newActualPathSegments = new string?[actualPathSegments.Length + key.AsSpan().Count(':') + 1];
        actualPathSegments.CopyTo(newActualPathSegments, 0);
        return newActualPathSegments;
    }
}
#endif
