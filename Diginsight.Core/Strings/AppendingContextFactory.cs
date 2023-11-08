using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace Diginsight.Strings;

internal sealed class AppendingContextFactory : IAppendingContextFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogStringOverallConfiguration overallConfiguration;

    public AppendingContextFactory(
        IServiceProvider serviceProvider,
        IOptions<LogStringOverallConfiguration> overallConfigurationOptions
    )
    {
        this.serviceProvider = serviceProvider;
        overallConfiguration = overallConfigurationOptions.Value;
    }

    public AppendingContext MakeAppendingContext(StringBuilder stringBuilder)
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
            serviceProvider.GetRequiredService<IMemberInfoLogStringProvider>(),
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

    public AppendingContextFactoryBuilder PrepareClone()
    {
        return new AppendingContextFactoryBuilder().Configure(overallConfiguration);
    }
}
