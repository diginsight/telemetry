using Diginsight.Options;
using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public class DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>
    : DynamicallyConfigureOptionsFromHttpRequestHeaders<TOptions>, IConfigureClassAwareOptions<TOptions>, IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    public DynamicallyConfigureClassAwareOptionsFromHttpRequestHeaders(
        string? name,
        IHttpContextAccessor httpContextAccessor
    )
        : base(name, httpContextAccessor) { }

    public void Configure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }

    public void PostConfigure(string name, Type @class, TOptions options)
    {
        ConfigureCore(name, options, configuration => FilteredConfiguration.For(configuration, @class));
    }
}
