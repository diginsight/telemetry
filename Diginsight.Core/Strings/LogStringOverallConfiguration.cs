using System.Text.RegularExpressions;

namespace Diginsight.Strings;

public sealed class LogStringOverallConfiguration : ILogStringOverallConfiguration
{
    private static readonly LogStringProviderRegistration[] FixedRegistrations;
    private static readonly Type[] FixedRegistrationTypes;
    private static readonly int MaxCustomRegistrationPriority;

    public IList<LogStringProviderRegistration> CustomRegistrations { get; } = new List<LogStringProviderRegistration>();

    public LogThreshold MaxCollectionItemCount { get; set; } = 20;

    public InheritableLogThreshold MaxDictionaryItemCount { get; set; } = 10;

    public InheritableLogThreshold MaxMemberwisePropertyCount { get; set; }

    public InheritableLogThreshold MaxAnonymousObjectPropertyCount { get; set; }

    public LogThreshold MaxTupleItemCount { get; set; } = 4;

    public LogThreshold MaxMethodParameterCount { get; set; } = 5;

    public LogThreshold MaxDepth { get; set; } = 5;

    public Regex? ImplicitNamespaces { get; set; }

    public Regex? ExplicitNamespaces { get; set; }

    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

    public TimeSpan MaxTime { get; set; } = TimeSpan.FromMilliseconds(5);

    public bool ShortenKnownTypes { get; set; } = true;

    public bool IsMemberwiseLogStringableByDefault { get; set; } = true;

    public StringComparison MetaPropertyKeyComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    static LogStringOverallConfiguration()
    {
        int minFixedRegistrationPriority;
        FixedRegistrations =
        [
            new LogStringProviderRegistration(typeof(ForbiddenLogStringProvider), int.MaxValue),
            new LogStringProviderRegistration(typeof(PrimitiveLogStringProvider), int.MaxValue - 1),
            new LogStringProviderRegistration(typeof(BasicLogStringProvider), int.MaxValue - 2),
            new LogStringProviderRegistration(typeof(IMemberInfoLogStringProvider), int.MaxValue - 3),
            new LogStringProviderRegistration(typeof(AnonymousLogStringProvider), int.MaxValue - 4),
            new LogStringProviderRegistration(typeof(JTokenLogStringProvider), minFixedRegistrationPriority = int.MaxValue - 5),
            new LogStringProviderRegistration(typeof(CollectionsLogStringProvider), int.MinValue + 1),
            new LogStringProviderRegistration(typeof(MemberwiseLogStringProvider), int.MinValue),
        ];
        FixedRegistrationTypes = FixedRegistrations.Select(static x => x.Type).ToArray();
        MaxCustomRegistrationPriority = minFixedRegistrationPriority - 1;
    }

    public IEnumerable<LogStringProviderRegistration> Registrations => CustomRegistrations
        .Select(static x => x.Priority > MaxCustomRegistrationPriority ? new LogStringProviderRegistration(x.Type, MaxCustomRegistrationPriority) : x)
        .Concat(FixedRegistrations)
#if NET6_0_OR_GREATER
        .DistinctBy(static x => x.Type);
#else
        .GroupBy(static x => x.Type, static (_, xs) => xs.First());
#endif

    public void ResetFrom(ILogStringOverallConfiguration source)
    {
        CustomRegistrations.Clear();
        CustomRegistrations.AddRange(
            source.Registrations
#if NET6_0_OR_GREATER
                .ExceptBy(FixedRegistrationTypes, static x => x.Type)
#else
                .Where(static x => !FixedRegistrationTypes.Contains(x.Type))
#endif
        );

        MaxCollectionItemCount = source.MaxCollectionItemCount;
        MaxDictionaryItemCount = source.MaxDictionaryItemCount;
        MaxMemberwisePropertyCount = source.MaxMemberwisePropertyCount;
        MaxAnonymousObjectPropertyCount = source.MaxAnonymousObjectPropertyCount;
        MaxTupleItemCount = source.MaxTupleItemCount;
        MaxMethodParameterCount = source.MaxMethodParameterCount;
        MaxDepth = source.MaxDepth;
        ImplicitNamespaces = source.ImplicitNamespaces;
        ExplicitNamespaces = source.ExplicitNamespaces;
        IsNamespaceExplicitIfUnspecified = source.IsNamespaceExplicitIfUnspecified;
        IsNamespaceExplicitIfAmbiguous = source.IsNamespaceExplicitIfAmbiguous;
        MaxTime = source.MaxTime;
        ShortenKnownTypes = source.ShortenKnownTypes;
        IsMemberwiseLogStringableByDefault = source.IsMemberwiseLogStringableByDefault;
        MetaPropertyKeyComparison = source.MetaPropertyKeyComparison;
    }
}
