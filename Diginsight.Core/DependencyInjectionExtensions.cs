using Diginsight.CAOptions;
using Diginsight.Strings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHostBuilder UseDiginsightServiceProvider(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.UseServiceProviderFactory(
            context =>
            {
                ServiceProviderOptions options = new ();
                configureOptions?.Invoke(context, options);
                return new DiginsightServiceProviderFactory(options);
            }
        );
    }

    public static IServiceCollection AddClassAwareOptions(this IServiceCollection services)
    {
        services.AddOptions();

        services.TryAddSingleton(typeof(IClassAwareOptions<>), typeof(UnnamedClassAwareOptionsManager<>));
        services.TryAddScoped(typeof(IClassAwareOptionsSnapshot<>), typeof(ClassAwareOptionsManager<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsMonitor<>), typeof(ClassAwareOptionsMonitor<>));
        services.TryAddTransient(typeof(IClassAwareOptionsFactory<>), typeof(ClassAwareOptionsFactory<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsCache<>), typeof(ClassAwareOptionsCache<>));

        if (ClassAwareOptions.OverrideClassAgnosticOptions)
        {
            services.Replace(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(ProxyClassAwareOptions<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<>), typeof(ProxyClassAwareOptionsSnapshot<>)));
            services.Replace(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(ProxyClassAwareOptionsMonitor<>)));
            services.Replace(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(ProxyClassAwareOptionsFactory<>)));
        }

        return services;
    }

    private sealed class ProxyClassAwareOptions<T> : IOptions<T>
        where T : class
    {
        private readonly IClassAwareOptions<T> underlying;

        public T Value => underlying.Value;

        public ProxyClassAwareOptions(IClassAwareOptions<T> underlying)
        {
            this.underlying = underlying;
        }
    }

    private sealed class ProxyClassAwareOptionsSnapshot<T> : IOptionsSnapshot<T>
        where T : class
    {
        private readonly IClassAwareOptionsSnapshot<T> underlying;

        public T Value => underlying.Value;

        public ProxyClassAwareOptionsSnapshot(IClassAwareOptionsSnapshot<T> underlying)
        {
            this.underlying = underlying;
        }

        public T Get(string? name) => underlying.Get(name);
    }

    private sealed class ProxyClassAwareOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly IClassAwareOptionsMonitor<T> underlying;

        public T CurrentValue => underlying.CurrentValue;

        public ProxyClassAwareOptionsMonitor(IClassAwareOptionsMonitor<T> underlying)
        {
            this.underlying = underlying;
        }

        public T Get(string? name) => underlying.Get(name);

        public IDisposable? OnChange(Action<T, string?> listener) => underlying.OnChange(listener);
    }

    private sealed class ProxyClassAwareOptionsFactory<T> : IOptionsFactory<T>
        where T : class
    {
        private readonly IClassAwareOptionsFactory<T> underlying;

        public ProxyClassAwareOptionsFactory(IClassAwareOptionsFactory<T> underlying)
        {
            this.underlying = underlying;
        }

        public T Create(string name) => underlying.Create(name);
    }

    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        return services.ConfigureClassAware<TOptions>(Options.DefaultName, configuration, sectionKey, configureBinder);
    }

    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, string name, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        services.AddClassAwareOptions();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>>(
                new ConfigureClassAwareOptionsFromConfiguration<TOptions>(name, configuration, sectionKey, configureBinder)
            )
        );

        return services;
    }

    public static IServiceCollection AddLogStrings(this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddSingleton<IAppendingContextFactory, AppendingContextFactory>();
        services.TryAddSingleton<IMemberInfoLogStringProvider, MemberInfoLogStringProvider>();
        services.TryAddSingleton<IReflectionLogStringHelper, ReflectionLogStringHelper>();

        return services;
    }

    public static IServiceCollection AddLoggerFactorySetter(this IServiceCollection services)
    {
        if (services.Any(static x => x.ServiceType == typeof(ILoggerFactorySetter)))
        {
            return services;
        }

        return services
            .AddLogging()
            .Decorate<ILoggerFactory, LoggerFactorySetter>()
            .AddSingleton(static p => (ILoggerFactorySetter)p.GetRequiredService<ILoggerFactory>());
    }
}
