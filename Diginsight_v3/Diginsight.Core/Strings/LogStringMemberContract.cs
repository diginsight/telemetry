using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.Strings;

public sealed class LogStringMemberContract
{
    public static readonly LogStringMemberContract Empty = new (null, null, null, false);

    private readonly Func<IServiceProvider, ILogStringProvider?> makeProvider;

    private ILogStringProvider? provider;
    private bool isInitialized;
    private object initLock = new ();

    public bool? Included { get; }
    public string? Name { get; }

    public LogStringMemberContract(bool? included, string? name, Type? providerType)
        : this(included, name, providerType, true) { }

    internal LogStringMemberContract(bool? included, string? name, Type? providerType, bool validateProvider)
    {
        if (validateProvider && providerType is not null && !typeof(ILogStringProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException($"Type '{providerType.Name}' is not assignable to {nameof(ILogStringProvider)}");
        }

        Included = included;
        Name = name;
        makeProvider = providerType is { } pt
            ? sp => (ILogStringProvider)ActivatorUtilities.CreateInstance(sp, pt)
            : static _ => null;
    }

    public ILogStringProvider? GetProvider(IServiceProvider serviceProvider)
    {
        return LazyInitializer.EnsureInitialized(ref provider, ref isInitialized, ref initLock, () => makeProvider(serviceProvider));
    }
}
