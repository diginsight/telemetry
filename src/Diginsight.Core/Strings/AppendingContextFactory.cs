using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

internal sealed class AppendingContextFactory : IAppendingContextFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogStringOverallConfiguration overallConfiguration;

    private IEnumerable<ILogStringProvider> LogStringProviders => LogStringOverallConfiguration.GetEffectiveRegistrations(overallConfiguration)
        .Select(static x => x ?? throw new ArgumentNullException($"Item in {nameof(ILogStringOverallConfiguration)}.{nameof(ILogStringOverallConfiguration.CustomRegistrations)}", (Exception?)null))
        .OrderByDescending(static x => x.Priority)
        .Select(x => (ILogStringProvider)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type));

    public AppendingContextFactory(
        IServiceProvider serviceProvider,
        IOptions<LogStringOverallConfiguration> overallConfigurationOptions
    )
    {
        this.serviceProvider = serviceProvider;
        overallConfiguration = overallConfigurationOptions.Value;
    }

    public AppendingContext MakeAppendingContext([NotNull] ref StringBuilder? stringBuilder)
    {
        return new AppendingContext(
            stringBuilder ??= new StringBuilder(),
            LogStringProviders.ToArray(),
            serviceProvider.GetRequiredService<IMemberInfoLogStringProvider>(),
            new LogStringVariableConfiguration(overallConfiguration),
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
    public AppendingContextFactoryBuilder PrepareClone()
    {
        return new AppendingContextFactoryBuilder().ConfigureOverall(overallConfiguration);
    }

    internal static ILogStringable ToLogStringable(object? obj, IEnumerable<ILogStringProvider> logStringProviders)
    {
        if (obj is null)
            return default(NullLogStringable);

        if (obj is ILogStringable logStringable0)
            return logStringable0;

        foreach (ILogStringProvider logStringProvider in logStringProviders)
        {
            if (logStringProvider.TryToLogStringable(obj) is { } logStringable1)
                return logStringable1;
        }

        return new NonLogStringable(obj.GetType());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ILogStringable ToLogStringable(object? obj) => ToLogStringable(obj, LogStringProviders);

    private readonly struct NullLogStringable : ILogStringable
    {
        bool ILogStringable.IsDeep => false;
        object? ILogStringable.Subject => null;

        public void AppendTo(AppendingContext appendingContext)
        {
            appendingContext.AppendDirect('□');
        }
    }
}
