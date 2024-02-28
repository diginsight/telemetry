using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.Strings;

public static class AppendingContextFactoryBuilderExtensions
{
    public static AppendingContextFactoryBuilder ConfigureOverall(
        this AppendingContextFactoryBuilder builder, ILogStringOverallConfiguration configuration
    )
    {
        return builder.ConfigureOverall(x => x.ResetFrom(configuration));
    }

    public static AppendingContextFactoryBuilder ConfigureOverall(
        this AppendingContextFactoryBuilder builder, Action<LogStringOverallConfiguration> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    public static AppendingContextFactoryBuilder ConfigureContracts(
        this AppendingContextFactoryBuilder builder, Action<LogStringTypeContractAccessor> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    public static AppendingContextFactoryBuilder RegisterProvider(
        this AppendingContextFactoryBuilder builder, Type providerType, int priority = 0
    )
    {
        builder.Services.Configure<LogStringOverallConfiguration>(
            configuration => { configuration.CustomRegistrations.Add(new LogStringProviderRegistration(providerType, priority)); }
        );
        return builder;
    }

    public static AppendingContextFactoryBuilder RegisterProvider<T>(
        this AppendingContextFactoryBuilder builder, int priority = 0
    )
        where T : ILogStringProvider
    {
        return builder.RegisterProvider(typeof(T), priority);
    }

    public static IAppendingContextFactory Build(this AppendingContextFactoryBuilder builder)
    {
        return builder.Services.BuildServiceProvider().GetRequiredService<IAppendingContextFactory>();
    }
}
