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

    extension(StringifyContextFactoryBuilder builder)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureOverall(
            IStringifyOverallConfiguration configuration
        )
        {
            return builder.ConfigureOverall(x => x.ResetFrom(configuration));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureOverall(
            Action<StringifyOverallConfiguration> configure
        )
        {
            builder.Services.Configure(configure);
            return builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureContracts(
            Action<StringifyTypeContractAccessor> configure
        )
        {
            builder.Services.Configure(configure);
            return builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder RegisterStringifier(
            Type stringifierType, int priority = 0
        )
        {
            builder.Services.Configure<StringifyOverallConfiguration>(
                configuration => { configuration.CustomRegistrations.Add(new StringifierRegistration(stringifierType, priority)); }
            );
            return builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder RegisterStringifier<T>(
            int priority = 0
        )
            where T : IStringifier
        {
            return builder.RegisterStringifier(typeof(T), priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder WithLoggerFactory(
            ILoggerFactory loggerFactory
        )
        {
            builder.Services.TryAddSingleton(loggerFactory);
            return builder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStringifyContextFactory Build()
        {
            return builder.Services.BuildServiceProvider().GetRequiredService<IStringifyContextFactory>();
        }
    }
}
