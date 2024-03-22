using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Strings;

public static class AppendingContextFactoryBuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContext MakeAppendingContext(
        this IAppendingContextFactory factory, out StringBuilder stringBuilder
    )
    {
        stringBuilder = null!;
        return factory.MakeAppendingContext(ref stringBuilder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContextFactoryBuilder ConfigureOverall(
        this AppendingContextFactoryBuilder builder, ILogStringOverallConfiguration configuration
    )
    {
        return builder.ConfigureOverall(x => x.ResetFrom(configuration));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContextFactoryBuilder ConfigureOverall(
        this AppendingContextFactoryBuilder builder, Action<LogStringOverallConfiguration> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContextFactoryBuilder ConfigureContracts(
        this AppendingContextFactoryBuilder builder, Action<LogStringTypeContractAccessor> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContextFactoryBuilder RegisterProvider(
        this AppendingContextFactoryBuilder builder, Type providerType, int priority = 0
    )
    {
        builder.Services.Configure<LogStringOverallConfiguration>(
            configuration => { configuration.CustomRegistrations.Add(new LogStringProviderRegistration(providerType, priority)); }
        );
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AppendingContextFactoryBuilder RegisterProvider<T>(
        this AppendingContextFactoryBuilder builder, int priority = 0
    )
        where T : ILogStringProvider
    {
        return builder.RegisterProvider(typeof(T), priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAppendingContextFactory Build(this AppendingContextFactoryBuilder builder)
    {
        return builder.Services.BuildServiceProvider().GetRequiredService<IAppendingContextFactory>();
    }
}
