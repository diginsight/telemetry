using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

public sealed class StringifyOverallConfiguration : IStringifyOverallConfiguration
{
    private static readonly StringifierRegistration[] FixedRegistrations;
    private static readonly int MaxCustomRegistrationPriority;

    public static IList<StringifierRegistration> GlobalCustomRegistrations { get; } = new List<StringifierRegistration>();

    private Threshold maxTotalLength = 300;

    public IList<StringifierRegistration> CustomRegistrations { get; } = new List<StringifierRegistration>();

    IEnumerable<StringifierRegistration> IStringifyOverallConfiguration.CustomRegistrations => CustomRegistrations;

    public Threshold MaxStringLength { get; set; } = 50;

    public Threshold MaxCollectionItemCount { get; set; } = 20;

    public InheritableThreshold MaxDictionaryItemCount { get; set; } = 10;

    public InheritableThreshold MaxMemberwisePropertyCount { get; set; }

    public InheritableThreshold MaxAnonymousObjectPropertyCount { get; set; }

    public Threshold MaxTupleItemCount { get; set; } = 4;

    public Threshold MaxMethodParameterCount { get; set; } = 5;

    public Threshold MaxDepth { get; set; } = 5;

    public Regex? ImplicitNamespaces { get; set; }

    public Regex? ExplicitNamespaces { get; set; }

    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

    public Expiration MaxTime { get; set; } = TimeSpan.FromMilliseconds(5);

    public Threshold MaxTotalLength
    {
        get => maxTotalLength;
        set => maxTotalLength = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxTotalLength), "Expected positive value") : value;
    }

    public bool ShortenKnownTypes { get; set; } = true;

    public bool IsMemberwiseStringifiableByDefault { get; set; } = true;

    public StringComparison MetaPropertyKeyComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    static StringifyOverallConfiguration()
    {
        int minFixedRegistrationPriority;
        FixedRegistrations =
        [
            new StringifierRegistration(typeof(ForbiddenStringifier), int.MaxValue),
            new StringifierRegistration(typeof(PrimitiveStringifier), int.MaxValue - 1),
            new StringifierRegistration(typeof(BasicStringifier), int.MaxValue - 2),
            new StringifierRegistration(typeof(IMemberInfoStringifier), int.MaxValue - 3),
            new StringifierRegistration(typeof(AnonymousStringifier), minFixedRegistrationPriority = int.MaxValue - 4),
            new StringifierRegistration(typeof(CollectionsStringifier), int.MinValue + 1),
            new StringifierRegistration(typeof(MemberwiseStringifier), int.MinValue),
        ];
        MaxCustomRegistrationPriority = minFixedRegistrationPriority - 1;
    }

    public static IEnumerable<StringifierRegistration> GetEffectiveRegistrations(IStringifyOverallConfiguration configuration)
    {
        return configuration.CustomRegistrations.Concat(GlobalCustomRegistrations)
            .Select(static x => x.Priority > MaxCustomRegistrationPriority ? new StringifierRegistration(x.Type, MaxCustomRegistrationPriority) : x)
            .Concat(FixedRegistrations)
#if NET
            .DistinctBy(static x => x.Type);
#else
            .GroupBy(static x => x.Type, static (_, xs) => xs.First());
#endif
    }

    public void ResetFrom(IStringifyOverallConfiguration source)
    {
        CustomRegistrations.Clear();
        CustomRegistrations.AddRange(source.CustomRegistrations);

        MaxStringLength = source.MaxStringLength;
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
        MaxTotalLength = source.MaxTotalLength;
        ShortenKnownTypes = source.ShortenKnownTypes;
        IsMemberwiseStringifiableByDefault = source.IsMemberwiseStringifiableByDefault;
        MetaPropertyKeyComparison = source.MetaPropertyKeyComparison;
    }
}
