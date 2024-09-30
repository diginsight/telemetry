using Diginsight.Logging;
using Diginsight.Options;
using Diginsight.Strings;
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

    public static IServiceCollection ConfigureClassAware<TOptions>(
        this IServiceCollection services, IConfiguration configuration, string? sectionKey = null, Action<BinderOptions>? configureBinder = null
    )
        where TOptions : class
    {
        return services.ConfigureClassAware<TOptions>(MseOptions.DefaultName, configuration, sectionKey, configureBinder);
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

    public static IServiceCollection ConfigureClassAwareOptions<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        return services.ConfigureClassAwareOptions(typeof(TOptions));
    }

    public static IServiceCollection ConfigureClassAwareOptions(this IServiceCollection services, Type type)
    {
        return services
            .ConfigureOptions(type)
            .ConfigureClassAwareOptions(type, t => ServiceDescriptor.Transient(t, type));
    }

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

    public static IServiceCollection AddVolatileConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton<IVolatileConfigurationStorageProvider, VolatileConfigurationStorageProvider>();
        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyConfigure<TOptions>(this IServiceCollection services)
        where TOptions: class, IVolatilelyConfigurable
    {
        return services.VolatilelyConfigure<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection VolatilelyConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IVolatilelyConfigurable
    {
        services
            .AddOptions()
            .AddVolatileConfiguration();

        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<VolatilelyConfigureOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureOptions<TOptions>>()
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureOptions<TOptions>>()
            )
        );

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions: class, IVolatilelyConfigurable
    {
        return services.VolatilelyConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection VolatilelyConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IVolatilelyConfigurable
    {
        services.AddClassAwareOptions();
        services.VolatilelyConfigure<TOptions>(name);

        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<VolatilelyConfigureClassAwareOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureClassAwareOptions<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureClassAwareOptions<TOptions>>()
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IClassAwareOptionsChangeTokenSource<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureClassAwareOptions<TOptions>>()
            )
        );

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyPostConfigure<TOptions>(this IServiceCollection services)
        where TOptions: class, IVolatilelyConfigurable
    {
        return services.VolatilelyPostConfigure<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection VolatilelyPostConfigure<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IVolatilelyConfigurable
    {
        services
            .AddOptions()
            .AddVolatileConfiguration();

        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<VolatilelyConfigureOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureOptions<TOptions>>()
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOptionsChangeTokenSource<TOptions>, VolatilelyConfigureOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureOptions<TOptions>>()
            )
        );

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IServiceCollection VolatilelyPostConfigureClassAware<TOptions>(this IServiceCollection services)
        where TOptions: class, IVolatilelyConfigurable
    {
        return services.VolatilelyPostConfigureClassAware<TOptions>(MseOptions.DefaultName);
    }

    public static IServiceCollection VolatilelyPostConfigureClassAware<TOptions>(this IServiceCollection services, string name)
        where TOptions: class, IVolatilelyConfigurable
    {
        services.AddClassAwareOptions();
        services.VolatilelyPostConfigure<TOptions>(name);

        services.TryAddSingleton(
            sp => ActivatorUtilities.CreateInstance<VolatilelyConfigureClassAwareOptions<TOptions>>(sp, name)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureClassAwareOptions<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureClassAwareOptions<TOptions>>()
            )
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IClassAwareOptionsChangeTokenSource<TOptions>, VolatilelyConfigureClassAwareOptions<TOptions>>(
                static sp => sp.GetRequiredService<VolatilelyConfigureClassAwareOptions<TOptions>>()
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
