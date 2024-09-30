using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Stringify;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContextFactoryBuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContext MakeStringifyContext(
        this IStringifyContextFactory factory, out StringBuilder stringBuilder
    )
    {
        stringBuilder = null!;
        return factory.MakeStringifyContext(ref stringBuilder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder ConfigureOverall(
        this StringifyContextFactoryBuilder builder, IStringifyOverallConfiguration configuration
    )
    {
        return builder.ConfigureOverall(x => x.ResetFrom(configuration));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder ConfigureOverall(
        this StringifyContextFactoryBuilder builder, Action<StringifyOverallConfiguration> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder ConfigureContracts(
        this StringifyContextFactoryBuilder builder, Action<StringifyTypeContractAccessor> configure
    )
    {
        builder.Services.Configure(configure);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder RegisterStringifier(
        this StringifyContextFactoryBuilder builder, Type stringifierType, int priority = 0
    )
    {
        builder.Services.Configure<StringifyOverallConfiguration>(
            configuration => { configuration.CustomRegistrations.Add(new StringifierRegistration(stringifierType, priority)); }
        );
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder RegisterStringifier<T>(
        this StringifyContextFactoryBuilder builder, int priority = 0
    )
        where T : IStringifier
    {
        return builder.RegisterStringifier(typeof(T), priority);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContextFactoryBuilder WithLoggerFactory(
        this StringifyContextFactoryBuilder builder, ILoggerFactory loggerFactory
    )
    {
        builder.Services.TryAddSingleton(loggerFactory);
        return builder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IStringifyContextFactory Build(this StringifyContextFactoryBuilder builder)
    {
        return builder.Services.BuildServiceProvider().GetRequiredService<IStringifyContextFactory>();
    }
}
