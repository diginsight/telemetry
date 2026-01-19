namespace Diginsight.Stringify;

public interface IStringifyOverallConfiguration : IStringifyVariableConfiguration
{
    IEnumerable<StringifierRegistration> CustomRegistrations { get; }
    Expiration MaxTime { get; }
    Threshold MaxTotalLength { get; }
    bool ShortenKnownTypes { get; }
    bool IsMemberwiseStringifiableByDefault { get; }
    StringComparison MetaPropertyKeyComparison { get; }

    void ResetFrom(IStringifyOverallConfiguration source);
}
