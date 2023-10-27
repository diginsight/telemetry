using Microsoft.Extensions.DependencyInjection;

namespace Diginsight.Strings;

public sealed class LogStringComposerBuilder
{
    public static LogStringComposerBuilder Default { get; set; } = new ();

    public IServiceCollection Services { get; } = new ServiceCollection();

    public LogStringComposerBuilder()
    {
        Services.AddLogStringComposer();
    }

    public LogStringComposerBuilder Configure(ILogStringOverallConfiguration configuration)
    {
        return Configure(x => x.ResetFrom(configuration));
    }

    public LogStringComposerBuilder Configure(Action<LogStringOverallConfiguration> configure)
    {
        Services.Configure(configure);
        return this;
    }

    public LogStringComposerBuilder RegisterProvider(Type providerType, int priority = 0)
    {
        Services.Configure<LogStringOverallConfiguration>(
            configuration => { configuration.CustomRegistrations.Add(new LogStringProviderRegistration(providerType, priority)); }
        );
        return this;
    }

    public LogStringComposerBuilder RegisterProvider<T>(int priority = 0)
        where T : ILogStringProvider
    {
        return RegisterProvider(typeof(T), priority);
    }

    public ILogStringComposer Build()
    {
        return Services.BuildServiceProvider().GetRequiredService<ILogStringComposer>();
    }
}
