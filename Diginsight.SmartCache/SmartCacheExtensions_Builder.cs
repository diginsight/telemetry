using Diginsight.AspNetCore;
using Diginsight.SmartCache.Externalization;
using Diginsight.SmartCache.Externalization.Kubernetes;
using Diginsight.SmartCache.Externalization.Local;
using Diginsight.SmartCache.Externalization.Middleware;
using Diginsight.SmartCache.Externalization.Redis;
using Diginsight.SmartCache.Externalization.ServiceBus;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache;

public static partial class SmartCacheExtensions
{
    public static SmartCacheBuilder SetCompanion(
        this SmartCacheBuilder builder, ICacheCompanionInstaller installer
    )
    {
        CacheCompanionUninstaller uninstaller;
        if (builder.Services.FirstOrDefault(static x => x.ServiceType == typeof(CacheCompanionUninstaller)) is { } uninstallerServiceDescriptor)
        {
            uninstaller = (CacheCompanionUninstaller)uninstallerServiceDescriptor.ImplementationInstance!;
        }
        else
        {
            uninstaller = new CacheCompanionUninstaller();
            builder.Services.AddSingleton(uninstaller);
        }

        uninstaller.Uninstall?.Invoke();
        installer.Install(builder.Services, out Action uninstall);
        uninstaller.Uninstall = uninstall;

        return builder;
    }

    private sealed class CacheCompanionUninstaller
    {
        public Action? Uninstall { get; set; }
    }

    public static SmartCacheBuilder SetLocalCompanion(this SmartCacheBuilder builder) =>
        builder.SetCompanion(LocalCacheCompanionInstaller.Instance);

    public static SmartCacheBuilder SetKubernetesCompanion(
        this SmartCacheBuilder builder,
        Action<SmartCacheKubernetesOptions>? configureKubernetesOptions = null,
        Action<SmartCacheMiddlewareOptions>? configureMiddlewareOptions = null
    )
    {
        builder
            .AddMiddleware(configureMiddlewareOptions)
            .SetCompanion(KubernetesCacheCompanionInstaller.Instance);

        if (configureKubernetesOptions is not null)
        {
            builder.Services.Configure(configureKubernetesOptions);
        }

        return builder;
    }

    public static SmartCacheBuilder SetServiceBusCompanion(
        this SmartCacheBuilder builder, Action<SmartCacheServiceBusOptions>? configureOptions = null
    )
    {
        builder.SetCompanion(ServiceBusCacheCompanionInstaller.Instance);

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    public static SmartCacheBuilder SetSizeLimit(this SmartCacheBuilder builder, long? sizeLimit)
    {
        builder.Services.Configure<MemoryCacheOptions>(nameof(SmartCache), x => { x.SizeLimit = sizeLimit; });
        return builder;
    }

    public static SmartCacheBuilder AddRedis(
        this SmartCacheBuilder builder, Action<SmartCacheRedisOptions>? configureOptions = null
    )
    {
        builder.Services.TryAddSingleton<IRedisDatabaseAccessor, RedisDatabaseAccessor>();
        builder.Services.TryAddSingleton<RedisCacheLocation>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheRedisOptions>, ValidateSmartCacheRedisOptions>());

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    private sealed class ValidateSmartCacheRedisOptions : IValidateOptions<SmartCacheRedisOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheRedisOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            if (options.Configuration is not null && string.IsNullOrEmpty(options.KeyPrefix))
            {
                return ValidateOptionsResult.Fail($"{nameof(SmartCacheRedisOptions.KeyPrefix)} must be non-empty");
            }

            return ValidateOptionsResult.Success;
        }
    }

    public static SmartCacheBuilder AddMiddleware(
        this SmartCacheBuilder builder, Action<SmartCacheMiddlewareOptions>? configureOptions = null
    )
    {
        builder.Services.TryAddTransient<SmartCacheMiddleware>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheMiddlewareOptions>, ValidateSmartCacheMiddlewareOptions>());

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    private sealed class ValidateSmartCacheMiddlewareOptions : IValidateOptions<SmartCacheMiddlewareOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheMiddlewareOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (options.RootPath?[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.RootPath)} must be non-empty and start with '/'");
            }
            if (options.GetPathSegment is { } getPathSegment && getPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.GetPathSegment)} must start with '/'");
            }
            if (options.CacheMissPathSegment is { } cacheMissPathSegment && cacheMissPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.CacheMissPathSegment)} must start with '/'");
            }
            if (options.InvalidatePathSegment is { } invalidatePathSegment && invalidatePathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheMiddlewareOptions.InvalidatePathSegment)} must start with '/'");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }

    public static SmartCacheBuilder AddHttpHeaderSupport(this SmartCacheBuilder builder)
    {
        builder.Services.PostConfigureClassAwareFromHttpRequestHeaders<SmartCacheCoreOptions>(static o => o.MakeFiller());
        builder.Services.PostConfigureClassAwareFromHttpRequestHeaders<OnTheFlySmartCacheCoreOptions>();

        return builder;
    }
}
