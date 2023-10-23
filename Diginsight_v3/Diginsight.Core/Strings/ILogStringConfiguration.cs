using System.Text.RegularExpressions;

namespace Diginsight.Strings;

public interface ILogStringConfiguration : ILogStringThresholdConfiguration
{
    IEnumerable<LogStringProviderRegistration> Registrations { get; }
    TimeSpan MaxTime { get; }
    Regex? ImplicitNamespaces { get; }
    Regex? ExplicitNamespaces { get; }
    bool IsNamespaceExplicitIfUnspecified { get; }
    bool IsNamespaceExplicitIfAmbiguous { get; }
    bool ShortenKnownTypes { get; }
    bool IsMemberwiseLogStringableByDefault { get; }
    StringComparison MetaPropertyKeyComparison { get; }

    void ResetFrom(ILogStringConfiguration source);
}
