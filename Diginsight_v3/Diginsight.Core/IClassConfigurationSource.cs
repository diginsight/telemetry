namespace Diginsight;

public interface IClassConfigurationSource
{
    bool TryGet<T>(IEnumerable<string> prefixes, string key, out T? value);
}
