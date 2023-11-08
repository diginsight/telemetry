using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

public sealed class AppendingContextFactoryBuilder
{
    private static IAppendingContextFactory? defaultFactory;

    public static AppendingContextFactoryBuilder DefaultBuilder { get; set; } = new ();

    [AllowNull]
    public static IAppendingContextFactory DefaultFactory
    {
        get => defaultFactory ??= DefaultBuilder.Build();
        set => defaultFactory = value;
    }

    public IServiceCollection Services { get; } = new ServiceCollection();

    public AppendingContextFactoryBuilder()
    {
        Services.AddLogStrings();
    }

    public AppendingContextFactoryBuilder Configure(ILogStringOverallConfiguration configuration)
    {
        return Configure(x => x.ResetFrom(configuration));
    }

    public AppendingContextFactoryBuilder Configure(Action<LogStringOverallConfiguration> configure)
    {
        Services.Configure(configure);
        return this;
    }

    public AppendingContextFactoryBuilder RegisterProvider(Type providerType, int priority = 0)
    {
        Services.Configure<LogStringOverallConfiguration>(
            configuration => { configuration.CustomRegistrations.Add(new LogStringProviderRegistration(providerType, priority)); }
        );
        return this;
    }

    public AppendingContextFactoryBuilder RegisterProvider<T>(int priority = 0)
        where T : ILogStringProvider
    {
        return RegisterProvider(typeof(T), priority);
    }

    public IAppendingContextFactory Build()
    {
        return Services.BuildServiceProvider().GetRequiredService<IAppendingContextFactory>();
    }
}
