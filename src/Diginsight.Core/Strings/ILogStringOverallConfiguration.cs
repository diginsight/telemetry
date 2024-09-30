namespace Diginsight.Strings;

public interface ILogStringOverallConfiguration : ILogStringVariableConfiguration
{
    IEnumerable<LogStringProviderRegistration> CustomRegistrations { get; }
    Expiration MaxTime { get; }
    LogThreshold MaxTotalLength { get; }
    bool ShortenKnownTypes { get; }
    bool IsMemberwiseLogStringableByDefault { get; }
    StringComparison MetaPropertyKeyComparison { get; }

    void ResetFrom(ILogStringOverallConfiguration source);

#if NET || NETSTANDARD2_1_OR_GREATER
    sealed int? GetEffectiveMaxTotalLength() => MaxTotalLength.Value;
#endif
}
