using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace Diginsight.Strings;

internal sealed class LogStringComposer : ILogStringComposer
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogStringConfiguration logStringConfiguration;

    public LogStringComposer(
        IServiceProvider serviceProvider,
        IOptions<LogStringConfiguration> logStringConfigurationOptions
    )
    {
        this.serviceProvider = serviceProvider;
        logStringConfiguration = logStringConfigurationOptions.Value;
    }

    // TODO MaxLength
    public void Append(
        object? obj,
        StringBuilder stringBuilder,
        Action<LogStringThresholdConfiguration>? configureThresholds = null,
        Action<IDictionary<string, object?>>? configureMetaProperties = null
    )
    {
        stringBuilder.AppendLogString(obj, MakeLoggingContext(), false, configureThresholds, configureMetaProperties);
    }

    private LoggingContext MakeLoggingContext()
    {
        ILogStringProvider[] logStringProviders = logStringConfiguration.Registrations
            .Select(static x => x ?? throw new ArgumentNullException($"item in {nameof(ILogStringConfiguration)}.{nameof(ILogStringConfiguration.Registrations)}", (Exception?)null))
            .OrderBy(static x => x.Priority)
            .Select(x => (ILogStringProvider)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type))
            .ToArray();

        LogStringThresholdConfiguration thresholdConfiguration = new LogStringThresholdConfiguration(logStringConfiguration);

        // TODO MaxTime
        return new LoggingContext(
            logStringProviders,
            thresholdConfiguration,
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            StringComparer.FromComparison(logStringConfiguration.MetaPropertyKeyComparison)
#else
            logStringConfiguration.MetaPropertyKeyComparison switch
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
        return new LogStringComposerBuilder().Configure(logStringConfiguration);
    }
}
