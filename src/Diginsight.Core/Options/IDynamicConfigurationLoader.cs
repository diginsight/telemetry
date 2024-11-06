namespace Diginsight.Options;

public interface IDynamicConfigurationLoader
{
    IEnumerable<KeyValuePair<string, string?>> Load();
}
