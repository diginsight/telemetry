using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

public class FilteredConfiguration<TClass> : IFilteredConfiguration
{
    private static readonly string[] Prefixes = ClassConfigurationPrefixes<TClass>.Prefixes.ToArray();

    private readonly IConfiguration underlying;
    private readonly string partialVirtualPath;

    public string? this[string key]
    {
        get => GetSection(key).Value;
        set => GetSection(key).Value = value;
    }

    Type IFilteredConfiguration.Class => typeof(TClass);

    public FilteredConfiguration(IConfiguration underlying, string partialVirtualPath = "")
    {
        this.underlying = underlying;
        this.partialVirtualPath = partialVirtualPath;
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
        static (string? Prefix, string VirtualKey) Split(string actualKey)
        {
            return actualKey.IndexOf('@') is > 0 and var idx ? (actualKey[..idx], actualKey[(idx + 1)..]) : (null, actualKey);
        }

        static (string Prefix, string VirtualKey, IConfigurationSection Section) Decompose(IConfigurationSection section)
        {
            string fullKey = section.Key;
            (string? prefix, string virtualKey) = Split(fullKey);
            return (prefix is null ? "" : $"{prefix}.", virtualKey, section);
        }

        static IConfigurationSection? ChooseBest(IEnumerable<(string Prefix, IConfigurationSection Section)> candidates)
        {
            int bestRank = int.MaxValue;
            IConfigurationSection? bestSection = null;

            foreach ((string prefix, IConfigurationSection section) in candidates)
            {
                int rank = Array.IndexOf(Prefixes, prefix);
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
            .GroupBy(static x => x.VirtualKey, static x => (x.Prefix, x.Section));

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
