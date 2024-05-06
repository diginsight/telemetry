namespace Diginsight;

public interface IDiginsightDistributedContextOptions
{
    IEnumerable<string> NonBaggageKeys { get; }
}
