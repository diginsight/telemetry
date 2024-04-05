using Diginsight.AspNetCore;
using Diginsight.SmartCache.Externalization.Kubernetes;
using Diginsight.SmartCache.Externalization.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache.Externalization;

public static class SmartCacheAspNetCoreExtensions
{
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
        builder.Services.PostConfigureClassAwareFromHttpRequestHeaders<SmartCacheCoreOptions>();

        return builder;
    }
}
