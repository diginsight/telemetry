using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Reflection;

namespace Diginsight.CAOptions;

public static class FilteredConfiguration
{
    public const char ClassDelimiter = '@';

    public static IConfiguration For(IConfiguration configuration, Type @class)
    {
        return (IConfiguration)typeof(FilteredConfiguration<>)
            .MakeGenericType(@class)
            .GetMethod(nameof(FilteredConfiguration<object>.For), BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [ configuration ])!;
    }
}

public class FilteredConfiguration<TClass> : IFilteredConfiguration
{
    private readonly IConfiguration underlying;
    private readonly string partialVirtualPath;

    public string? this[string key]
    {
        get => GetSection(key).Value;
        set => GetSection(key).Value = value;
    }

    Type IFilteredConfiguration.Class => typeof(TClass);

    internal FilteredConfiguration(IConfiguration underlying, string partialVirtualPath = "")
    {
        this.underlying = underlying;
        this.partialVirtualPath = partialVirtualPath;
    }

    public static IConfiguration For(IConfiguration configuration)
    {
        return configuration switch
        {
            IFilteredConfiguration filtered =>
                filtered.Class == typeof(TClass) ? filtered : throw new ArgumentException("Configuration already filtered on another class"),
            IConfigurationRoot root => new FilteredConfigurationRoot<TClass>(root),
            IConfigurationSection section => new FilteredConfigurationSection<TClass>(section),
            _ => new FilteredConfiguration<TClass>(configuration),
        };
    }

    public IConfigurationSection GetSection(string key)
    {
        string[] segments = key
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            .Split(ConfigurationPath.KeyDelimiter);
#else
            .Split([ ConfigurationPath.KeyDelimiter ], StringSplitOptions.None);
#endif
        return segments.Skip(1).Aggregate(CoreGetChild(segments[0]), static (s, k) => s.CoreGetChild(k));
    }

    private FilteredConfigurationSection<TClass> CoreGetChild(string key)
    {
        return CoreGetChildren(key).FirstOrDefault()
            ?? new FilteredConfigurationSection<TClass>(underlying.GetSection(key), partialVirtualPath + key);
    }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return CoreGetChildren().ToArray();
    }

    private IEnumerable<FilteredConfigurationSection<TClass>> CoreGetChildren(string? virtualKey = null)
    {
        static (string? Marker, string VirtualKey) Split(string actualKey)
        {
            return actualKey.IndexOf(FilteredConfiguration.ClassDelimiter) is > 0 and var idx ? (actualKey[(idx + 1)..], actualKey[..idx]) : (null, actualKey);
        }

        static (string Marker, string VirtualKey, IConfigurationSection Section) Decompose(IConfigurationSection section)
        {
            string fullKey = section.Key;
            (string? marker, string virtualKey) = Split(fullKey);
            return (marker ?? "", virtualKey, section);
        }

        IEnumerable<string> markers = ClassConfigurationMarkers<TClass>.Markers;

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
                    ? new FilteredConfigurationSection<TClass>(best, partialVirtualPath + g.Key)
                    : null
            )
            .OfType<FilteredConfigurationSection<TClass>>();
    }

    public IChangeToken GetReloadToken() => underlying.GetReloadToken();
}
