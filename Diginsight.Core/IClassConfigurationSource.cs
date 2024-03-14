#if EXPERIMENT_CLASS_CONFIGURATION_GETTER
namespace Diginsight;

public interface IClassConfigurationSource
{
    void PopulateAll<T>(IEnumerable<string> prefixes, string key, IDictionary<string, T> dict, IClassConfigurationGetter.SafeConverter<T>? tryConvert);

    bool TryGet<T>(IEnumerable<string> prefixes, string key, out T value, IClassConfigurationGetter.SafeConverter<T>? tryConvert);
}
#endif
