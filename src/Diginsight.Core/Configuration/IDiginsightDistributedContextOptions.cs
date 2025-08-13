namespace Diginsight;

/// <summary>
/// Interface for distributed context options in Diginsight, used by <see cref="DiginsightPropagator" />.
/// </summary>
public interface IDiginsightDistributedContextOptions
{
    /// <summary>
    /// Gets the collection of keys that flow in the distributed context, but not in the baggage property of the carrier.
    /// </summary>
    IEnumerable<string> NonBaggageKeys { get; }
}
