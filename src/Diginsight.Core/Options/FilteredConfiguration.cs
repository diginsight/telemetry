using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace Diginsight.Options;

public class FilteredConfiguration : IFilteredConfiguration
{
    public const char ClassDelimiter = '@';

    private readonly IConfiguration underlying;
    private readonly string partialVirtualPath;

    public string? this[string key]
    {
        get => GetSection(key).Value;
        set => GetSection(key).Value = value;
    }

    public Type Class { get; }

    internal FilteredConfiguration(IConfiguration underlying, Type @class, string partialVirtualPath = "")
    {
        this.underlying = underlying;
        Class = @class;
        this.partialVirtualPath = partialVirtualPath;
    }

    public static IFilteredConfiguration For(IConfiguration configuration, Type? @class)
    {
        @class ??= ClassAwareOptions.NoClass;
        return configuration switch
        {
            IFilteredConfiguration filtered =>
                filtered.Class == @class ? filtered : throw new ArgumentException("Configuration already filtered on another class"),
            IConfigurationRoot root => new FilteredConfigurationRoot(root, @class),
            IConfigurationSection section => new FilteredConfigurationSection(section, @class),
            _ => new FilteredConfiguration(configuration, @class),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFilteredConfiguration ForNoClass(IConfiguration configuration) => For(configuration, null);

    /// <inheritdoc />
    public IConfigurationSection GetSection(string key)
    {
        string[] segments = key
#if NET || NETSTANDARD2_1_OR_GREATER
            .Split(ConfigurationPath.KeyDelimiter);
#else
            .Split([ ConfigurationPath.KeyDelimiter ], StringSplitOptions.None);
#endif
        return segments.Skip(1).Aggregate(CoreGetChild(segments[0]), static (s, k) => s.CoreGetChild(k));
    }

    private FilteredConfigurationSection CoreGetChild(string key)
    {
        return CoreGetChildren(key).FirstOrDefault()
            ?? new FilteredConfigurationSection(underlying.GetSection(key), Class, partialVirtualPath + key);
    }

    /// <inheritdoc />
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return CoreGetChildren().ToArray();
    }

    private IEnumerable<FilteredConfigurationSection> CoreGetChildren(string? virtualKey = null)
    {
        static (string? Marker, string VirtualKey) Split(string actualKey)
        {
            return actualKey.IndexOf(ClassDelimiter) is > 0 and var idx ? (actualKey[(idx + 1)..], actualKey[..idx]) : (null, actualKey);
        }

        static (string Marker, string VirtualKey, IConfigurationSection Section) Decompose(IConfigurationSection section)
        {
            string fullKey = section.Key;
            (string? marker, string virtualKey) = Split(fullKey);
            return (marker ?? "", virtualKey, section);
        }

        IEnumerable<string> markers = ClassConfigurationMarkers.For(Class);

        IConfigurationSection? ChooseBest(IEnumerable<(string Marker, IConfigurationSection Section)> candidates)
        {
            int bestRank = int.MaxValue;
            IConfigurationSection? bestSection = null;

            foreach ((string marker, IConfigurationSection section) in candidates)
            {
                int rank = markers.FirstIndexWhere(x => x.Equals(marker, StringComparison.OrdinalIgnoreCase));
                if (rank < 0 || rank >= bestRank)
                    continue;

                bestRank = rank;
                bestSection = section;
            }

            return bestSection;
        }

        var groupings = underlying
            .GetChildren()
            .Select(Decompose)
            .GroupBy(static x => x.VirtualKey, static x => (x.Marker, x.Section));

        return (virtualKey is null ? groupings : groupings.Where(g => string.Equals(g.Key, virtualKey, StringComparison.OrdinalIgnoreCase)))
            .Select(
                g => ChooseBest(g.AsEnumerable()) is { } best
                    ? new FilteredConfigurationSection(best, Class, partialVirtualPath + g.Key)
                    : null
            )
            .OfType<FilteredConfigurationSection>();
    }

    /// <inheritdoc />
    public IChangeToken GetReloadToken() => underlying.GetReloadToken();
}
