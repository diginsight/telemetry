namespace Diginsight;

public sealed class OptionsCacheSettings
{
    public ISet<(Type, string?)> DynamicEntries { get; } = new HashSet<(Type, string?)>();
}
