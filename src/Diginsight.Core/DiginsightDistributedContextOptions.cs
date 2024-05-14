namespace Diginsight;

public sealed class DiginsightDistributedContextOptions : IDiginsightDistributedContextOptions
{
    public ISet<string> NonBaggageKeys { get; } = new HashSet<string>();

    IEnumerable<string> IDiginsightDistributedContextOptions.NonBaggageKeys => NonBaggageKeys;
}
