#if EXPERIMENT_FILTERED_CONFIGURATION
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

internal sealed class FilteredConfigurationRoot : IConfigurationRoot
{
    private readonly IConfigurationRoot underlying;
    private readonly Type @class;

    public IEnumerable<IConfigurationProvider> Providers => underlying.Providers;

    public string? this[string key]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public FilteredConfigurationRoot(IConfigurationRoot underlying, Type @class)
    {
        this.underlying = underlying;
        this.@class = @class;
    }

    public IConfigurationSection GetSection(string key) => throw new NotImplementedException();

    public IEnumerable<IConfigurationSection> GetChildren() => CoreGetChildren("", Array.Empty<string?>());

    public IChangeToken GetReloadToken() => underlying.GetReloadToken();

    public void Reload() => underlying.Reload();

    public string? CoreGetValue(string virtualPath, string?[] actualPathSegments)
    {
        throw new NotImplementedException();
    }

    public void CoreSetValue(string virtualPath, string?[] actualPathSegments, string? value)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IConfigurationSection> CoreGetChildren(string virtualPath, string?[] actualPathSegments)
    {
        //static (string Prefix, string Key, IConfigurationSection Section) Decompose(IConfigurationSection section)
        //{
        //    string fullKey = section.Key;
        //    (string? prefix, Split(fullKey, out string? prefix, out string key);
        //    return (prefix is null ? "" : $"{prefix}.", key, section);
        //}

        //IReadOnlyDictionary<string, IReadOnlyDictionary<string, IConfigurationSection>> dictionary = underlying
        //    .GetChildren()
        //    .Select(Decompose)
        //    .GroupBy(static x => x.Key, static x => (x.Prefix, x.Section))
        //    .ToDictionary(
        //        static g => g.Key,
        //        static g => (IReadOnlyDictionary<string, IConfigurationSection>)g.ToDictionary(static x => x.Prefix, static x => x.Section)
        //    );
        throw new NotImplementedException();
    }

    public static (string? Prefix, string VirtualKey) Split(string actualKey)
    {
        return actualKey.IndexOf('@') is > 0 and var idx ? (actualKey[..(idx - 1)], actualKey[(idx + 1)..]) : (null, actualKey);
    }
}
#endif
