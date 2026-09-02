using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Diginsight.Stringify;

/// <summary>
/// Provides extension methods for configuring stringify context factory builders.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StringifyContextFactoryBuilderExtensions
{
    /// <summary>
    /// Creates a stringify context.
    /// </summary>
    /// <param name="factory">The stringify context factory.</param>
    /// <param name="stringBuilder">When this method returns, contains the string builder used by the context.</param>
    /// <returns>The created stringify context.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringifyContext MakeStringifyContext(
        this IStringifyContextFactory factory, out StringBuilder stringBuilder
    )
    {
        stringBuilder = null!;
        return factory.MakeStringifyContext(ref stringBuilder);
    }

    /// <param name="builder">The StringifyContextFactoryBuilder instance.</param>
    extension(StringifyContextFactoryBuilder builder)
    {
        /// <summary>
        /// Configures the overall stringify options.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureOverall(
            IStringifyOverallConfiguration configuration
        )
        {
            return builder.ConfigureOverall(x => x.ResetFrom(configuration));
        }

        /// <summary>
        /// Configures the overall stringify options.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureOverall(
            Action<StringifyOverallConfiguration> configure
        )
        {
            builder.Services.Configure(configure);
            return builder;
        }

        /// <summary>
        /// Configures the stringify contracts.
        /// </summary>
        /// <param name="configure">The configuration action.</param>
        /// <returns>The builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder ConfigureContracts(
            Action<StringifyTypeContractAccessor> configure
        )
        {
            builder.Services.Configure(configure);
            return builder;
        }

        /// <summary>
        /// Registers a custom stringifier.
        /// </summary>
        /// <param name="stringifierType">The stringifier type.</param>
        /// <param name="priority">The registration priority.</param>
        /// <returns>The builder.</returns>
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

        /// <summary>
        /// Registers a custom stringifier.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="priority">The registration priority.</param>
        /// <returns>The builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder RegisterStringifier<T>(
            int priority = 0
        )
            where T : IStringifier
        {
            return builder.RegisterStringifier(typeof(T), priority);
        }

        /// <summary>
        /// Registers a logger factory for stringification services.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns>The builder.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringifyContextFactoryBuilder WithLoggerFactory(
            ILoggerFactory loggerFactory
        )
        {
            builder.Services.TryAddSingleton(loggerFactory);
            return builder;
        }

        /// <summary>
        /// Builds a stringify context factory.
        /// </summary>
        /// <returns>The built stringify context factory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStringifyContextFactory Build()
        {
            return builder.Services.BuildServiceProvider().GetRequiredService<IStringifyContextFactory>();
        }
    }
}
