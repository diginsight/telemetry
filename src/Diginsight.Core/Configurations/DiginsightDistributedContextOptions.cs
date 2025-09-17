namespace Diginsight;

/// <summary>
/// Class for distributed context options in Diginsight, used by <see cref="DiginsightPropagator" />.
/// </summary>
public sealed class DiginsightDistributedContextOptions : IDiginsightDistributedContextOptions
{
    /// <inheritdoc cref="IDiginsightDistributedContextOptions.NonBaggageKeys" />
    public ISet<string> NonBaggageKeys { get; } = new HashSet<string>();

    /// <inheritdoc />
    IEnumerable<string> IDiginsightDistributedContextOptions.NonBaggageKeys => NonBaggageKeys;
}
