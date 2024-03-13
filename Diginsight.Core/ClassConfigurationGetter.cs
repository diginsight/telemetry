using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Diginsight;

internal class ClassConfigurationGetter : IClassConfigurationGetter
{
    private readonly IConfiguration configuration;
    private readonly IEnumerable<IClassConfigurationSource> classConfigurationSources;
    private readonly IDictionary<string, StrongBox<object?>?> cache = new Dictionary<string, StrongBox<object?>?>();

    public static IClassConfigurationGetter Empty => default(EmptyClassConfigurationGetter);

    public static IClassConfigurationGetterProvider EmptyProvider => default(EmptyClassConfigurationGetterProvider);

    public static IClassConfigurationGetter<TClass> EmptyFor<TClass>() => default(EmptyClassConfigurationGetter<TClass>);

    protected virtual IEnumerable<string> Prefixes => [ "" ];

    public ClassConfigurationGetter(
        IConfiguration configuration,
        IEnumerable<IClassConfigurationSource> classConfigurationSources,
        ConfigurationWrapper? configurationWrapper = null
    )
    {
        this.configuration = configurationWrapper?.Configuration ?? configuration;
        this.classConfigurationSources = classConfigurationSources;
    }

    public IEnumerable<T> GetAll<T>(string key, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
    {
        IDictionary<string, T> dict = new Dictionary<string, T>();
        foreach (string prefix in Prefixes)
        {
            string fullKey = prefix + key;
            if (configuration.GetSection(fullKey).Value is null)
            {
                continue;
            }

            try
            {
                dict[prefix] = configuration.GetValue<T>(fullKey)!;
            }
            catch (Exception e)
            {
                _ = e;
            }
        }

        foreach (IClassConfigurationSource classConfigurationSource in classConfigurationSources.Reverse())
        {
            classConfigurationSource.PopulateAll(Prefixes, key, dict, tryConvert);
        }

        return Prefixes.Intersect(dict.Keys).Select(x => dict[x]);
    }

    public bool TryGet<T>(string key, out T value, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
    {
        foreach (IClassConfigurationSource classConfigurationSource in classConfigurationSources)
        {
            if (classConfigurationSource.TryGet(Prefixes, key, out value, tryConvert))
            {
                return true;
            }
        }

        bool TryGetCore(out T value)
        {
            foreach (string fullKey in Prefixes.Select(x => x + key))
            {
                if (configuration.GetSection(fullKey).Value is null)
                {
                    continue;
                }

                try
                {
                    value = configuration.GetValue<T>(fullKey)!;
                    return true;
                }
                catch (Exception e)
                {
                    _ = e;
                }
            }

            value = default!;
            return false;
        }

        StrongBox<object?>? box;
        lock (((ICollection)cache).SyncRoot)
        {
            if (!cache.TryGetValue(key, out box))
            {
                box = cache[key] = TryGetCore(out value) ? new StrongBox<object?>(value) : null;
            }
        }

        if (box is null)
        {
            value = default!;
            return false;
        }

        value = (T)box.Value!;
        return true;
    }

    private readonly struct EmptyClassConfigurationGetter : IClassConfigurationGetter
    {
        public bool TryGet<T>(string key, out T value, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
        {
            value = default!;
            return false;
        }

        public IEnumerable<T> GetAll<T>(string key, IClassConfigurationGetter.SafeConverter<T>? tryConvert = null)
        {
            return Enumerable.Empty<T>();
        }
    }

    private readonly struct EmptyClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
    {
        public bool TryGet<T>(string key, out T value, IClassConfigurationGetter.SafeConverter<T>? tryConvert)
        {
            value = default!;
            return false;
        }

        public IEnumerable<T> GetAll<T>(string key, IClassConfigurationGetter.SafeConverter<T>? tryConvert = null)
        {
            return Enumerable.Empty<T>();
        }
    }

    private readonly struct EmptyClassConfigurationGetterProvider : IClassConfigurationGetterProvider
    {
        public IClassConfigurationGetter GetFor(Type @class)
        {
            return (IClassConfigurationGetter)Activator.CreateInstance(typeof(EmptyClassConfigurationGetter<>).MakeGenericType(@class))!;
        }
    }

    internal sealed class ConfigurationWrapper
    {
        public IConfiguration Configuration { get; }

        public ConfigurationWrapper(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}

internal sealed class ClassConfigurationGetter<TClass> : ClassConfigurationGetter, IClassConfigurationGetter<TClass>
{
    protected override IEnumerable<string> Prefixes => ClassConfigurationPrefixes<TClass>.Prefixes;

    public ClassConfigurationGetter(
        IConfiguration configuration,
        IEnumerable<IClassConfigurationSource> classConfigurationSources,
        ConfigurationWrapper? configurationWrapper = null
    )
        : base(configuration, classConfigurationSources, configurationWrapper) { }
}
