using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Stringify;

internal sealed class StringifyContextFactory : IStringifyContextFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly IStringifyOverallConfiguration overallConfiguration;

    private IEnumerable<IStringifier> Stringifiers => StringifyOverallConfiguration.GetEffectiveRegistrations(overallConfiguration)
        .Select(static x => x ?? throw new ArgumentNullException($"Item in {nameof(IStringifyOverallConfiguration)}.{nameof(IStringifyOverallConfiguration.CustomRegistrations)}", (Exception?)null))
        .OrderByDescending(static x => x.Priority)
        .Select(x => (IStringifier)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type));

    public StringifyContextFactory(
        IServiceProvider serviceProvider,
        IOptions<StringifyOverallConfiguration> overallConfigurationOptions
    )
    {
        this.serviceProvider = serviceProvider;
        overallConfiguration = overallConfigurationOptions.Value;
    }

    public StringifyContext MakeStringifyContext([NotNull] ref StringBuilder? stringBuilder)
    {
        return new StringifyContext(
            stringBuilder ??= new StringBuilder(),
            Stringifiers.ToArray(),
            serviceProvider.GetRequiredService<IMemberInfoStringifier>(),
            new StringifyVariableConfiguration(overallConfiguration),
            overallConfiguration.MaxTime,
            overallConfiguration.GetEffectiveMaxTotalLength(),
#if NET || NETSTANDARD2_1_OR_GREATER
            StringComparer.FromComparison(overallConfiguration.MetaPropertyKeyComparison)
#else
            overallConfiguration.MetaPropertyKeyComparison switch
            {
                StringComparison.CurrentCulture => StringComparer.CurrentCulture,
                StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
                StringComparison.InvariantCulture => StringComparer.InvariantCulture,
                StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
                StringComparison.Ordinal => StringComparer.Ordinal,
                StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
                _ => throw new ArgumentException($"unrecognized {nameof(StringComparison)}"),
            }
#endif
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringifyContextFactoryBuilder PrepareClone()
    {
        return new StringifyContextFactoryBuilder().ConfigureOverall(overallConfiguration);
    }

    internal static IStringifiable ToStringifiable(object? obj, IEnumerable<IStringifier> stringifiers)
    {
        if (obj is null)
            return default(NullStringifiable);

        if (obj is IStringifiable stringifiable0)
            return stringifiable0;

        foreach (IStringifier stringifier in stringifiers)
        {
            if (stringifier.TryStringify(obj) is { } stringifiable1)
                return stringifiable1;
        }

        return new NonStringifiable(obj.GetType());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IStringifiable ToStringifiable(object? obj) => ToStringifiable(obj, Stringifiers);

    private readonly struct NullStringifiable : IStringifiable
    {
        bool IStringifiable.IsDeep => false;
        object? IStringifiable.Subject => null;

        public void AppendTo(StringifyContext stringifyContext)
        {
            stringifyContext.AppendDirect('□');
        }
    }
}
