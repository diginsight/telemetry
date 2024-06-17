using Diginsight.CAOptions;
using Microsoft.AspNetCore.Http;

namespace Diginsight.AspNetCore;

public class ConfigureClassAwareOptionsFromHttpRequestHeaders<TOptions>
    : ConfigureOptionsFromHttpRequestHeaders<TOptions>, IConfigureClassAwareOptions<TOptions>, IPostConfigureClassAwareOptions<TOptions>
    where TOptions : class, IDynamicallyConfigurable
{
    public ConfigureClassAwareOptionsFromHttpRequestHeaders(
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
