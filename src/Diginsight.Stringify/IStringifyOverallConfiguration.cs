namespace Diginsight.Stringify;

/// <summary>
/// Represents overall stringify configuration.
/// </summary>
public interface IStringifyOverallConfiguration : IStringifyVariableConfiguration
{
    /// <summary>
    /// Gets the custom stringifier registrations.
    /// </summary>
    IEnumerable<StringifierRegistration> CustomRegistrations { get; }
    /// <summary>
    /// Gets the maximum allotted stringification time.
    /// </summary>
    Expiration MaxTime { get; }
    /// <summary>
    /// Gets the maximum total output length.
    /// </summary>
    Threshold MaxTotalLength { get; }
    /// <summary>
    /// Gets a value indicating whether known type names are shortened.
    /// </summary>
    bool ShortenKnownTypes { get; }
    /// <summary>
    /// Gets a value indicating whether objects are memberwise stringifiable by default.
    /// </summary>
    bool IsMemberwiseStringifiableByDefault { get; }
    /// <summary>
    /// Gets the comparison used for meta property keys.
    /// </summary>
    StringComparison MetaPropertyKeyComparison { get; }

    /// <summary>
    /// Resets this configuration from another configuration instance.
    /// </summary>
    /// <param name="source">The source configuration.</param>
    void ResetFrom(IStringifyOverallConfiguration source);
}
