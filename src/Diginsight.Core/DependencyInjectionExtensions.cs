using Diginsight.Logging;
using Diginsight.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MseOptions = Microsoft.Extensions.Options.Options;

namespace Diginsight;

/// <summary>
/// Provides extension methods for dependency injection and configuration services.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configures the <see cref="IHostBuilder" /> to use the Diginsight service provider.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configureOptions">The action to configure <see cref="ServiceProviderOptions" />.</param>
    /// <returns>The host builder, for chaining.</returns>
#if NET9_0_OR_GREATER
    [OverloadResolutionPriority(-1)]
#endif
    [Obsolete("Use the overload with the additional `bool validateInDevelopment` argument instead")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHostBuilder UseDiginsightServiceProvider(
        this IHostBuilder hostBuilder,
        Action<HostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.UseDiginsightServiceProvider(false, configureOptions);
    }

    /// <summary>
    /// Configures the <see cref="IHostBuilder" /> to use the Diginsight service provider.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configureOptions">The action to configure <see cref="ServiceProviderOptions" />.</param>
    /// <returns>The host builder, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IHostBuilder UseDiginsightServiceProvider(
        this IHostBuilder hostBuilder,
#if NET9_0_OR_GREATER
        bool validateInDevelopment = true,
#else
        bool validateInDevelopment,
#endif
        Action<HostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.UseServiceProviderFactory(
            context =>
            {
                bool validate = validateInDevelopment && context.HostingEnvironment.IsDevelopment();
                ServiceProviderOptions options = new () { ValidateScopes = validate, ValidateOnBuild = validate };
                configureOptions?.Invoke(context, options);
                return new DiginsightServiceProviderFactory(options);
            }
        );
    }

    /// <summary>
    /// Configures the <see cref="IHostApplicationBuilder" /> to use the Diginsight service provider.
    /// </summary>
    /// <param name="hostBuilder">The host application builder.</param>
    /// <param name="configureOptions">The action to configure <see cref="ServiceProviderOptions" />.</param>
    /// <returns>The host application builder, for chaining.</returns>
    public static IHostApplicationBuilder UseDiginsightServiceProvider(
        this IHostApplicationBuilder hostBuilder,
        bool validateInDevelopment = true,
        Action<HostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        IHostEnvironment environment = hostBuilder.Environment;
        HostBuilderContext hbc = new (hostBuilder.Properties)
        {
            Configuration = hostBuilder.Configuration,
            HostingEnvironment = environment,
        };

        bool validate = validateInDevelopment && environment.IsDevelopment();
        ServiceProviderOptions spo = new () { ValidateScopes = validate, ValidateOnBuild = validate };
        configureOptions?.Invoke(hbc, spo);

        hostBuilder.ConfigureContainer(new DiginsightServiceProviderFactory(spo));

        return hostBuilder;
    }

    /// <summary>
    /// Flags the specified options type as dynamic, in order to prevent caching.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection FlagAsDynamic<TOptions>(this IServiceCollection services, string? name)
        where TOptions : class, IDynamicallyConfigurable
    {
        services.AddOptions();

        if (!services.Any(static x => x.ServiceType == typeof(IOptionsMonitorCache<TOptions>)))
        {
            ServiceDescriptor cacheDescriptor = services.First(static x => x.ServiceType == typeof(IOptionsMonitorCache<>));
            Type cacheType = cacheDescriptor.ImplementationType!.MakeGenericType(typeof(TOptions));

            services.TryAddSingleton(cacheType);
            services.TryAddSingleton<IOptionsMonitorCache<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<ExcludableOptionsCache<TOptions>>(sp, sp.GetRequiredService(cacheType))
            );
        }

        services.TryAddSingleton<OptionsCache<TOptions>>();
        services.TryAddSingleton<IOptionsMonitorCache<TOptions>, ExcludableOptionsCache<TOptions>>();

        OptionsCacheSettings settings;
        if (services.FirstOrDefault(static x => x.ServiceType == typeof(OptionsCacheSettings)) is { } settingsDescriptor)
        {
            settings = (OptionsCacheSettings)settingsDescriptor.ImplementationInstance!;
        }
        else
        {
            settings = new OptionsCacheSettings();
            services.AddSingleton(settings);
        }

        settings.DynamicEntries.Add((typeof(TOptions), name));

        return services;
    }

    private sealed class ExcludableOptionsCache<TOptions> : IOptionsMonitorCache<TOptions>
        where TOptions : class
    {
        private readonly IOptionsMonitorCache<TOptions> decoratee;
        private readonly OptionsCacheSettings settings;

        public ExcludableOptionsCache(IOptionsMonitorCache<TOptions> decoratee, OptionsCacheSettings? settings = null)
        {
            this.decoratee = decoratee;
            this.settings = settings ?? new OptionsCacheSettings();
        }

        private bool IsDynamic(string name)
        {
            ISet<(Type, string?)> set = settings.DynamicEntries;
            return set.Contains((typeof(TOptions), null)) || set.Contains((typeof(TOptions), name));
        }

        public TOptions GetOrAdd(string? name, Func<TOptions> createOptions)
        {
            name ??= MseOptions.DefaultName;
            return IsDynamic(name) ? createOptions() : decoratee.GetOrAdd(name, createOptions);
        }

        public bool TryAdd(string? name, TOptions options)
        {
            name ??= MseOptions.DefaultName;
            return IsDynamic(name) ? throw new ArgumentException("Dynamic option cannot be cached") : decoratee.TryAdd(name, options);
        }

        public bool TryRemove(string? name) => decoratee.TryRemove(name);

        public void Clear() => decoratee.Clear();
    }

    /// <summary>
    /// Adds class-aware options services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddClassAwareOptions(this IServiceCollection services)
    {
        services.AddOptions();

        services.TryAddSingleton(typeof(IClassAwareOptions<>), typeof(UnnamedClassAwareOptionsManager<>));
        services.TryAddScoped(typeof(IClassAwareOptionsSnapshot<>), typeof(ClassAwareOptionsManager<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsMonitor<>), typeof(ClassAwareOptionsMonitor<>));
        services.TryAddTransient(typeof(IClassAwareOptionsFactory<>), typeof(ClassAwareOptionsFactory<>));
        services.TryAddSingleton(typeof(IClassAwareOptionsCache<>), typeof(ClassAwareOptionsCache<>));

        if (ClassAwareOptions.ReplaceClassAgnosticOptions)
        {
            services.Replace(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(ProxyClassAwareOptions<>)));
            services.Replace(ServiceDescriptor.Scoped(typeof(IOptionsSnapshot<>), typeof(ProxyClassAwareOptionsSnapshot<>)));
            services.Replace(ServiceDescriptor.Singleton(typeof(IOptionsMonitor<>), typeof(ProxyClassAwareOptionsMonitor<>)));
            services.Replace(ServiceDescriptor.Transient(typeof(IOptionsFactory<>), typeof(ProxyClassAwareOptionsFactory<>)));
        }

        return services;
    }

    private sealed class ProxyClassAwareOptions<TOptions> : IOptions<TOptions>
        where TOptions : class
    {
        private readonly IClassAwareOptions<TOptions> underlying;

        public TOptions Value => underlying.Value;

        public ProxyClassAwareOptions(IClassAwareOptions<TOptions> underlying)
        {
            this.underlying = underlying;
        }
    }

    private sealed class ProxyClassAwareOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
        where TOptions : class
    {
        private readonly IClassAwareOptionsSnapshot<TOptions> underlying;

        public TOptions Value => underlying.Value;

        public ProxyClassAwareOptionsSnapshot(IClassAwareOptionsSnapshot<TOptions> underlying)
        {
            this.underlying = underlying;
        }

        public TOptions Get(string? name) => underlying.Get(name);
    }

    private sealed class ProxyClassAwareOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly IClassAwareOptionsMonitor<TOptions> underlying;

        public TOptions CurrentValue => underlying.CurrentValue;

        public ProxyClassAwareOptionsMonitor(IClassAwareOptionsMonitor<TOptions> underlying)
        {
            this.underlying = underlying;
        }

        public TOptions Get(string? name) => underlying.Get(name);

        public IDisposable? OnChange(Action<TOptions, string?> listener) => underlying.OnChange(listener);
    }

    private sealed class ProxyClassAwareOptionsFactory<TOptions> : IOptionsFactory<TOptions>
        where TOptions : class
    {
        private readonly IClassAwareOptionsFactory<TOptions> underlying;

        public ProxyClassAwareOptionsFactory(IClassAwareOptionsFactory<TOptions> underlying)
        {
            this.underlying = underlying;
        }

        public TOptions Create(string name) => underlying.Create(name);
    }

    /// <summary>
    /// Configures class-aware options from the specified <see cref="IConfiguration" />.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionKey">The section key in the configuration, or <c>null</c> to read from the object itself.</param>
    /// <param name="configureBinder">The action to configure <see cref="BinderOptions" />.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        return services.ConfigureClassAware<TOptions>(MseOptions.DefaultName, configuration, sectionKey, configureBinder);
    }

    /// <summary>
    /// Configures named class-aware options from the specified <see cref="IConfiguration" />.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionKey">The section key in the configuration, or <c>null</c> to read from the object itself.</param>
    /// <param name="configureBinder">The action to configure <see cref="BinderOptions" />.</param>
    /// <returns>The service collection, for chaining.</returns>
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

    /// <summary>
    /// Configures class-aware options from the specified type.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection ConfigureClassAwareOptions<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        return services.ConfigureClassAwareOptions(typeof(TOptions));
    }

    /// <summary>
    /// Configures class-aware options from the specified type.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="type">The type to configure.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection ConfigureClassAwareOptions(this IServiceCollection services, Type type)
    {
        return services
            .ConfigureOptions(type)
            .ConfigureClassAwareOptions(type, t => ServiceDescriptor.Transient(t, type));
    }

    /// <summary>
    /// Configures class-aware options from the specified instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The instance to configure.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection ConfigureClassAwareOptions(this IServiceCollection services, object instance)
    {
        return services
            .ConfigureOptions(instance)
            .ConfigureClassAwareOptions(instance.GetType(), t => ServiceDescriptor.Singleton(t, instance));
    }

    private static IServiceCollection ConfigureClassAwareOptions(
        this IServiceCollection services, Type type, Func<Type, ServiceDescriptor> makeDescriptor
    )
    {
        services.AddClassAwareOptions();

        foreach (Type ifc in type.GetInterfaces())
        {
            if (!ifc.IsGenericType)
            {
                continue;
            }

            Type ifcDefinition = ifc.GetGenericTypeDefinition();
            if (ifcDefinition == typeof(IConfigureClassAwareOptions<>) ||
                ifcDefinition == typeof(IPostConfigureClassAwareOptions<>) ||
                ifcDefinition == typeof(IValidateClassAwareOptions<>))
            {
                services.Add(makeDescriptor(ifc));
            }
        }

        return services;
    }

    /// <summary>
    /// Adds volatile configuration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddVolatileConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton<IVolatileConfigurationStorageProvider, VolatileConfigurationStorageProvider>();
        return services;
    }

    /// <summary>
    /// Enables the specified options type for volatile configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyConfigure<TOptions>(this IServiceCollection services)
        where TOptions : class, IVolatilelyConfigurable
    {
        return services.VolatilelyConfigure<TOptions>(MseOptions.DefaultName);
    }

    /// <summary>
    /// Enables the specified options type for named volatile configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection VolatilelyConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IVolatilelyConfigurable
    {
        services
            .AddOptions()
            .AddVolatileConfiguration();

        object nonce = new VolatilelyConfigureNonce();
        services.TryAddKeyedSingleton(
            nonce, (sp, _) => ActivatorUtilities.CreateInstance<VolatilelyConfigureOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureOptions<TOptions>>(nonce)
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureOptions<TOptions>>(nonce)
            )
        );

        return services;
    }

    private sealed record VolatilelyConfigureNonce
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>
    /// Enables the specified options type for class-aware volatile configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions : class, IVolatilelyConfigurable
    {
        return services.VolatilelyConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    /// <summary>
    /// Enables the specified options type for named class-aware volatile configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection VolatilelyConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IVolatilelyConfigurable
    {
        services.AddClassAwareOptions();
        services.VolatilelyConfigure<TOptions>(name);

        object nonce = new VolatilelyConfigureClassAwareNonce();
        services.TryAddKeyedSingleton(
            nonce, (sp, _) => ActivatorUtilities.CreateInstance<VolatilelyConfigureClassAwareOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureClassAwareOptions<TOptions>>(nonce)
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IClassAwareOptionsChangeTokenSource<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureClassAwareOptions<TOptions>>(nonce)
            )
        );

        return services;
    }

    private sealed record VolatilelyConfigureClassAwareNonce
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>
    /// Enables the specified options type for volatile post-configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyPostConfigure<TOptions>(this IServiceCollection services)
        where TOptions : class, IVolatilelyConfigurable
    {
        return services.VolatilelyPostConfigure<TOptions>(MseOptions.DefaultName);
    }

    /// <summary>
    /// Enables the specified options type for named volatile post-configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection VolatilelyPostConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IVolatilelyConfigurable
    {
        services
            .AddOptions()
            .AddVolatileConfiguration();

        object nonce = new VolatilelyPostConfigureNonce();
        services.TryAddKeyedSingleton(
            nonce, (sp, _) => ActivatorUtilities.CreateInstance<VolatilelyConfigureOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureOptions<TOptions>>(nonce)
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureOptions<TOptions>>(nonce)
            )
        );

        return services;
    }

    private sealed record VolatilelyPostConfigureNonce
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    /// <summary>
    /// Enables the specified options type for class-aware volatile post-configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyPostConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions : class, IVolatilelyConfigurable
    {
        return services.VolatilelyPostConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    /// <summary>
    /// Enables the specified options type for class-aware volatile named post-configuration.
    /// </summary>
    /// <typeparam name="TOptions">The type of options.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the options instance.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection VolatilelyPostConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IVolatilelyConfigurable
    {
        services.AddClassAwareOptions();
        services.VolatilelyPostConfigure<TOptions>(name);

        object nonce = new VolatilelyPostConfigureClassAwareNonce();
        services.TryAddKeyedSingleton(
            nonce, (sp, _) => ActivatorUtilities.CreateInstance<VolatilelyConfigureClassAwareOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureClassAwareOptions<TOptions>>(nonce)
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IClassAwareOptionsChangeTokenSource<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                sp => sp.GetRequiredKeyedService<VolatilelyConfigureClassAwareOptions<TOptions>>(nonce)
            )
        );

        return services;
    }

    private sealed record VolatilelyPostConfigureClassAwareNonce
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyConfigure<TOptions>(this IServiceCollection services)
        where TOptions : class, IDynamicallyConfigurable
    {
        return services.DynamicallyConfigure<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection DynamicallyConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IDynamicallyConfigurable
    {
        services.AddOptions();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, DynamicallyConfigureOptions<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureOptions<TOptions>>(sp, name)
            )
        );

        services.FlagAsDynamic<TOptions>(name);

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions : class, IDynamicallyConfigurable
    {
        return services.DynamicallyConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection DynamicallyConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IDynamicallyConfigurable
    {
        services.AddClassAwareOptions();
        services.DynamicallyConfigure<TOptions>(name);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>, DynamicallyConfigureClassAwareOptions<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureClassAwareOptions<TOptions>>(sp, name)
            )
        );

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyPostConfigure<TOptions>(this IServiceCollection services)
        where TOptions : class, IDynamicallyConfigurable
    {
        return services.DynamicallyPostConfigure<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection DynamicallyPostConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IDynamicallyConfigurable
    {
        services.AddOptions();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, DynamicallyConfigureOptions<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureOptions<TOptions>>(sp, name)
            )
        );

        services.FlagAsDynamic<TOptions>(name);

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection DynamicallyPostConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions : class, IDynamicallyConfigurable
    {
        return services.DynamicallyPostConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection DynamicallyPostConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions : class, IDynamicallyConfigurable
    {
        services.AddClassAwareOptions();
        services.DynamicallyPostConfigure<TOptions>(name);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, DynamicallyConfigureClassAwareOptions<TOptions>>(
                sp => ActivatorUtilities.CreateInstance<DynamicallyConfigureClassAwareOptions<TOptions>>(sp, name)
            )
        );

        return services;
    }

    /// <summary>
    /// Adds the <see cref="ILoggerFactorySetter" /> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
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
