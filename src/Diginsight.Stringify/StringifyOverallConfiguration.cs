using System.Text.RegularExpressions;

namespace Diginsight.Stringify;

/// <summary>
/// Represents global and default stringification configuration.
/// </summary>
public sealed class StringifyOverallConfiguration : IStringifyOverallConfiguration
{
    private static readonly StringifierRegistration[] FixedRegistrations;
    private static readonly int MaxCustomRegistrationPriority;

    /// <summary>
    /// Gets the global custom stringifier registrations.
    /// </summary>
    public static IList<StringifierRegistration> GlobalCustomRegistrations { get; } = new List<StringifierRegistration>();

    /// <summary>
    /// Gets the custom stringifier registrations.
    /// </summary>
    public IList<StringifierRegistration> CustomRegistrations { get; } = new List<StringifierRegistration>();

    IEnumerable<StringifierRegistration> IStringifyOverallConfiguration.CustomRegistrations => CustomRegistrations;

    /// <summary>
    /// Gets the maximum string length.
    /// </summary>
    public Threshold MaxStringLength { get; set; } = 50;

    /// <summary>
    /// Gets the maximum number of collection items.
    /// </summary>
    public Threshold MaxCollectionItemCount { get; set; } = 20;

    /// <summary>
    /// Gets the maximum number of dictionary items.
    /// </summary>
    public InheritableThreshold MaxDictionaryItemCount { get; set; } = 10;

    /// <summary>
    /// Gets the maximum number of memberwise properties.
    /// </summary>
    public InheritableThreshold MaxMemberwisePropertyCount { get; set; }

    /// <summary>
    /// Gets the maximum number of anonymous object properties.
    /// </summary>
    public InheritableThreshold MaxAnonymousObjectPropertyCount { get; set; }

    /// <summary>
    /// Gets the maximum number of tuple items.
    /// </summary>
    public Threshold MaxTupleItemCount { get; set; } = 4;

    /// <summary>
    /// Gets the maximum number of method parameters.
    /// </summary>
    public Threshold MaxMethodParameterCount { get; set; } = 5;

    /// <summary>
    /// Gets the maximum nesting depth.
    /// </summary>
    public Threshold MaxDepth { get; set; } = 5;

    /// <summary>
    /// Gets the namespace pattern whose matches can be rendered implicitly.
    /// </summary>
    public Regex? ImplicitNamespaces { get; set; }

    /// <summary>
    /// Gets the namespace pattern whose matches must be rendered explicitly.
    /// </summary>
    public Regex? ExplicitNamespaces { get; set; }

    /// <summary>
    /// Gets a value indicating whether unspecified namespaces are rendered explicitly.
    /// </summary>
    public bool IsNamespaceExplicitIfUnspecified { get; set; }

    /// <summary>
    /// Gets a value indicating whether ambiguous namespaces are rendered explicitly.
    /// </summary>
    public bool IsNamespaceExplicitIfAmbiguous { get; set; }

    /// <summary>
    /// Gets the maximum allotted stringification time.
    /// </summary>
    public Expiration MaxTime { get; set; } = TimeSpan.FromMilliseconds(5);

    /// <summary>
    /// Gets the maximum total output length.
    /// </summary>
    public Threshold MaxTotalLength
    {
        get;
        set => field = value.Value == 0 ? throw new ArgumentOutOfRangeException(nameof(MaxTotalLength), "Expected positive value") : value;
    } = 300;

    /// <summary>
    /// Gets a value indicating whether known type names are shortened.
    /// </summary>
    public bool ShortenKnownTypes { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether objects are memberwise stringifiable by default.
    /// </summary>
    public bool IsMemberwiseStringifiableByDefault { get; set; } = true;

    /// <summary>
    /// Gets the comparison used for meta property keys.
    /// </summary>
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
            new StringifierRegistration(typeof(AnonymousStringifier), int.MaxValue - 4),
            new StringifierRegistration(typeof(JsonNodeStringifier), minFixedRegistrationPriority = int.MaxValue - 5),
            new StringifierRegistration(typeof(CollectionsStringifier), int.MinValue + 1),
            new StringifierRegistration(typeof(MemberwiseStringifier), int.MinValue),
        ];
        MaxCustomRegistrationPriority = minFixedRegistrationPriority - 1;
    }

    /// <summary>
    /// Gets the effective stringifier registrations for a configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The effective stringifier registrations.</returns>
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

    /// <summary>
    /// Resets this configuration from another configuration instance.
    /// </summary>
    /// <param name="source">The source configuration.</param>
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
