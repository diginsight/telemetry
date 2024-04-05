using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Diginsight.SmartCache.Externalization.Http;

public static class SmartCacheHttpExtensions
{
    public static SmartCacheBuilder AddHttp(
        this SmartCacheBuilder builder, Action<SmartCacheHttpOptions>? configureOptions = null
    )
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SmartCacheHttpOptions>, ValidateSmartCacheHttpOptions>());

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    private sealed class ValidateSmartCacheHttpOptions : IValidateOptions<SmartCacheHttpOptions>
    {
        public ValidateOptionsResult Validate(string? name, SmartCacheHttpOptions options)
        {
            if (name != Options.DefaultName)
            {
                return ValidateOptionsResult.Skip;
            }

            ICollection<string> failureMessages = new List<string>();
            if (options.RootPath?[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheHttpOptions.RootPath)} must be non-empty and start with '/'");
            }
            if (options.GetPathSegment is { } getPathSegment && getPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheHttpOptions.GetPathSegment)} must start with '/'");
            }
            if (options.CacheMissPathSegment is { } cacheMissPathSegment && cacheMissPathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheHttpOptions.CacheMissPathSegment)} must start with '/'");
            }
            if (options.InvalidatePathSegment is { } invalidatePathSegment && invalidatePathSegment[0] != '/')
            {
                failureMessages.Add($"{nameof(SmartCacheHttpOptions.InvalidatePathSegment)} must start with '/'");
            }

            return failureMessages.Count > 0 ? ValidateOptionsResult.Fail(failureMessages) : ValidateOptionsResult.Success;
        }
    }
}
