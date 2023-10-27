namespace Diginsight.Strings;

public interface ILogStringOverallConfiguration : ILogStringVariableConfiguration
{
    IEnumerable<LogStringProviderRegistration> Registrations { get; }
    TimeSpan MaxTime { get; }
    bool ShortenKnownTypes { get; }
    bool IsMemberwiseLogStringableByDefault { get; }
    StringComparison MetaPropertyKeyComparison { get; }

    void ResetFrom(ILogStringOverallConfiguration source);
}
