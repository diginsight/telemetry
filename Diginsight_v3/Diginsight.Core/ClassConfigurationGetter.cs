using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Reflection;

namespace Diginsight;

public static class ClassConfigurationGetter
{
    public static IClassConfigurationGetter Empty => default(EmptyClassConfigurationGetter);

    public static IClassConfigurationGetterProvider EmptyProvider => default(EmptyClassConfigurationGetterProvider);

    public static IClassConfigurationGetter<TClass> EmptyFor<TClass>() => default(EmptyClassConfigurationGetter<TClass>);

    private readonly struct EmptyClassConfigurationGetter : IClassConfigurationGetter
    {
        public T? Get<T>(string key, T? defaultValue) => defaultValue;
    }

    private readonly struct EmptyClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
    {
        public T? Get<T>(string key, T? defaultValue) => defaultValue;
    }

    private readonly struct EmptyClassConfigurationGetterProvider : IClassConfigurationGetterProvider
    {
        public IClassConfigurationGetter GetFor(Type @class)
        {
            return (IClassConfigurationGetter)Activator.CreateInstance(typeof(EmptyClassConfigurationGetter<>).MakeGenericType(@class))!;
        }
    }
}

internal sealed class ClassConfigurationGetter<TClass> : IClassConfigurationGetter<TClass>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly string[] Prefixes;

    private readonly IConfiguration configuration;
    private readonly IEnumerable<IClassConfigurationSource> classConfigurationSources;
    private readonly IDictionary<string, object?> cache = new Dictionary<string, object?>();

    static ClassConfigurationGetter()
    {
        Prefixes = GetPrefixes().ToArray();

        static IEnumerable<string> GetPrefixes()
        {
            Type type = typeof(TClass);

            string[] namespacePieces = (type.Namespace ?? "").Split('.');
            IEnumerable<string> namespaceSegments = Enumerable.Range(1, namespacePieces.Length).Select(i => string.Join(".", namespacePieces.Take(i))).Reverse().ToArray();

            var availableShorthands = type.Assembly.GetCustomAttributes<ClassConfigurationNamespaceShorthandAttribute>().ToDictionary(static x => x.Namespace, static x => x.Shorthand);

            IEnumerable<string> namespaceShorthands = namespaceSegments.Select(x => availableShorthands.TryGetValue(x, out var val) ? val : null).OfType<string>().ToArray();

            if (type.FullName != null)
            {
                yield return $"{type.FullName}.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.{type.Name}.";
            }

            yield return $"{type.Name}.";

            foreach (string segment in namespaceSegments)
            {
                yield return $"{segment}.*.";
            }

            foreach (string shorthand in namespaceShorthands)
            {
                yield return $"#{shorthand}.*.";
            }

            yield return "";
        }
    }

    public ClassConfigurationGetter(
        IConfiguration configuration,
        IEnumerable<IClassConfigurationSource> classConfigurationSources
    )
    {
        this.configuration = configuration;
        this.classConfigurationSources = classConfigurationSources;
    }

    public T? Get<T>(string key, T? defaultValue)
    {
        foreach (IClassConfigurationSource classConfigurationSource in classConfigurationSources)
        {
            if (classConfigurationSource.TryGet(Prefixes, key, out T? value))
            {
                return value;
            }
        }

        T? CoreGet()
        {
            foreach (string fullKey in Prefixes.Select(x => x + key))
            {
                if (configuration.GetSection(fullKey).Value is null)
                {
                    continue;
                }

                try
                {
                    return configuration.GetValue<T>(fullKey);
                }
                catch (Exception e)
                {
                    _ = e;
                }
            }

            return defaultValue;
        }

        lock (((ICollection)cache).SyncRoot)
        {
            if (cache.TryGetValue(key, out object? rawValue))
            {
                return (T?)rawValue;
            }

            T? finalValue = CoreGet();
            cache[key] = finalValue;
            return finalValue;
        }
    }
}
