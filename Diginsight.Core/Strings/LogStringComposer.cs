using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace Diginsight.Strings;

// FIXME LogStringComposer
internal sealed class LogStringComposer : ILogStringComposer
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogStringOverallConfiguration overallConfiguration;

    public LogStringComposer(
        IServiceProvider serviceProvider,
        IOptions<LogStringOverallConfiguration> overallConfigurationOptions
    )
    {
        this.serviceProvider = serviceProvider;
        overallConfiguration = overallConfigurationOptions.Value;
    }

    // TODO MaxLength
    public void ComposeTo(
        object? obj,
        StringBuilder stringBuilder,
        Action<LogStringVariableConfiguration>? configureVariables = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        MakeAppendingContext(stringBuilder).ComposeAndAppend(obj, false, configureVariables, configureMetaProperties);
    }

    private AppendingContext MakeAppendingContext(StringBuilder stringBuilder)
    {
        ILogStringProvider[] logStringProviders = overallConfiguration.Registrations
            .Select(static x => x ?? throw new ArgumentNullException($"item in {nameof(ILogStringOverallConfiguration)}.{nameof(ILogStringOverallConfiguration.Registrations)}", (Exception?)null))
            .OrderByDescending(static x => x.Priority)
            .Select(x => (ILogStringProvider)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type))
            .ToArray();

        LogStringVariableConfiguration variableConfiguration = new LogStringVariableConfiguration(overallConfiguration);

        return new AppendingContext(
            stringBuilder,
            logStringProviders,
            variableConfiguration,
            overallConfiguration.MaxTime,
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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

    public LogStringComposerBuilder PrepareClone()
    {
        return new LogStringComposerBuilder().Configure(overallConfiguration);
    }
}
